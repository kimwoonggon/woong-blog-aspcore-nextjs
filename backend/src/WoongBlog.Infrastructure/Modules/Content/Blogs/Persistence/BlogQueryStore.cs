using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;
using WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogById;
using WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogs;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogDetailContext;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Common;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Infrastructure.Modules.Content.Blogs.Persistence;

public sealed class BlogQueryStore(WoongBlogDbContext dbContext) : IBlogQueryStore
{
    private const string PostgresProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const int MaxDetailContextLimit = 24;
    private static readonly ConcurrentDictionary<IModel, Func<WoongBlogDbContext, string, IAsyncEnumerable<BlogDetailDto>>> PublishedDetailBySlugQueries = new();

    private static Func<WoongBlogDbContext, string, IAsyncEnumerable<BlogDetailDto>> CreatePublishedDetailBySlugQuery(IModel _)
    {
        return EF.CompileAsyncQuery((WoongBlogDbContext context, string slug) =>
            context.Blogs.AsNoTracking()
                .Where(x => x.Slug == slug && x.Published)
                .Select(blog => new BlogDetailDto(
                    blog.Id,
                    blog.Slug,
                    blog.Title,
                    blog.Excerpt,
                    PublicContentBodyDto.FromStoredFields(blog.PublicContentHtml, blog.PublicContentMarkdown),
                    blog.Tags,
                    blog.PublicCoverUrl,
                    blog.PublishedAt))
                .Take(1));
    }

    public async Task<IReadOnlyList<AdminBlogListItemDto>> GetAdminListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Blogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminBlogListItemDto(
                x.Id,
                x.Title,
                x.Slug,
                x.Excerpt,
                x.Tags,
                x.Published,
                x.PublishedAt,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminBlogDetailDto?> GetAdminDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Blogs
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AdminBlogDetailDto(
                x.Id,
                x.Title,
                x.Slug,
                x.Excerpt,
                x.Tags,
                x.Published,
                x.PublishedAt,
                x.UpdatedAt,
                new AdminBlogContentDto(AdminContentJson.ExtractHtml(x.ContentJson))))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedBlogsDto> GetPublishedPageAsync(
        int page,
        int pageSize,
        string? normalizedQuery,
        ContentSearchMode searchMode,
        CancellationToken cancellationToken)
    {
        if (ShouldUsePostgresFirstPageWindowQuery(page, normalizedQuery))
        {
            return await GetPublishedFirstPageWithWindowAsync(pageSize, cancellationToken);
        }

        var query = ApplySearch(
                dbContext.Blogs.AsNoTracking().Where(x => x.Published),
                normalizedQuery,
                searchMode)
            .OrderByDescending(x => x.PublishedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var resolvedPage = Math.Min(page, totalPages);

        var blogs = await query
            .Skip((resolvedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(blog => new BlogCardRow(
                blog.Id,
                blog.Slug,
                blog.Title,
                blog.Excerpt,
                blog.Tags,
                blog.PublicCoverUrl,
                blog.PublishedAt))
            .ToListAsync(cancellationToken);

        var items = blogs.Select(blog => new BlogCardDto(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            blog.Tags,
            blog.PublicCoverUrl,
            blog.PublishedAt)).ToList();

        return new PagedBlogsDto(items, resolvedPage, pageSize, totalItems, totalPages);
    }

    private bool ShouldUsePostgresFirstPageWindowQuery(int page, string? normalizedQuery)
    {
        return page == 1
            && normalizedQuery is null
            && string.Equals(dbContext.Database.ProviderName, PostgresProviderName, StringComparison.Ordinal);
    }

    private async Task<PagedBlogsDto> GetPublishedFirstPageWithWindowAsync(
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database
            .SqlQuery<BlogCardWithTotalRow>(
                $"""
                SELECT
                    b."Id",
                    b."Slug",
                    b."Title",
                    b."Excerpt",
                    b."Tags",
                    b."PublicCoverUrl" AS "CoverUrl",
                    b."PublishedAt",
                    COUNT(*) OVER()::integer AS "TotalItems"
                FROM "Blogs" AS b
                WHERE b."Published" = TRUE
                ORDER BY b."PublishedAt" DESC
                LIMIT {pageSize}
                """)
            .ToListAsync(cancellationToken);

        var totalItems = rows.Count == 0 ? 0 : rows[0].TotalItems;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var items = rows.Select(blog => new BlogCardDto(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            blog.Tags,
            blog.CoverUrl,
            blog.PublishedAt)).ToList();

        return new PagedBlogsDto(items, 1, pageSize, totalItems, totalPages);
    }

    private sealed record BlogCardRow(
        Guid Id,
        string Slug,
        string Title,
        string Excerpt,
        string[] Tags,
        string PublicCoverUrl,
        DateTimeOffset? PublishedAt);

    private sealed class BlogCardWithTotalRow
    {
        public Guid Id { get; init; }
        public string Slug { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Excerpt { get; init; } = string.Empty;
        public string[] Tags { get; init; } = [];
        public string CoverUrl { get; init; } = string.Empty;
        public DateTimeOffset? PublishedAt { get; init; }
        public int TotalItems { get; init; }
    }

    public async Task<BlogDetailDto?> GetPublishedDetailBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var query = PublishedDetailBySlugQueries.GetOrAdd(dbContext.Model, CreatePublishedDetailBySlugQuery);
        await foreach (var blog in query(dbContext, slug).WithCancellation(cancellationToken))
        {
            return blog;
        }

        return null;
    }

    public async Task<BlogDetailContextDto?> GetPublishedDetailContextBySlugAsync(
        string slug,
        int limit,
        CancellationToken cancellationToken)
    {
        var current = await dbContext.Blogs
            .AsNoTracking()
            .Where(blog => blog.Slug == slug && blog.Published)
            .Select(blog => new BlogContextCurrentRow(blog.Id, blog.Title, blog.PublishedAt))
            .SingleOrDefaultAsync(cancellationToken);

        if (current is null)
        {
            return null;
        }

        var relatedLimit = Math.Clamp(limit, 0, MaxDetailContextLimit);
        var publishedBlogs = dbContext.Blogs
            .AsNoTracking()
            .Where(blog => blog.Published && blog.Id != current.Id);
        var currentPublishedAt = current.PublishedAt ?? DateTimeOffset.MinValue;

        var newer = await publishedBlogs
            .Where(blog =>
                (blog.PublishedAt ?? DateTimeOffset.MinValue) > currentPublishedAt
                || ((blog.PublishedAt ?? DateTimeOffset.MinValue) == currentPublishedAt
                    && string.Compare(blog.Title, current.Title) < 0))
            .OrderBy(blog => blog.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(blog => blog.Title)
            .Select(blog => new BlogCardDto(
                blog.Id,
                blog.Slug,
                blog.Title,
                blog.Excerpt,
                blog.Tags,
                blog.PublicCoverUrl,
                blog.PublishedAt))
            .FirstOrDefaultAsync(cancellationToken);

        var older = await publishedBlogs
            .Where(blog =>
                (blog.PublishedAt ?? DateTimeOffset.MinValue) < currentPublishedAt
                || ((blog.PublishedAt ?? DateTimeOffset.MinValue) == currentPublishedAt
                    && string.Compare(blog.Title, current.Title) > 0))
            .OrderByDescending(blog => blog.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenBy(blog => blog.Title)
            .Select(blog => new BlogCardDto(
                blog.Id,
                blog.Slug,
                blog.Title,
                blog.Excerpt,
                blog.Tags,
                blog.PublicCoverUrl,
                blog.PublishedAt))
            .FirstOrDefaultAsync(cancellationToken);

        var newerRelated = await publishedBlogs
            .Where(blog =>
                (blog.PublishedAt ?? DateTimeOffset.MinValue) > currentPublishedAt
                || ((blog.PublishedAt ?? DateTimeOffset.MinValue) == currentPublishedAt
                    && string.Compare(blog.Title, current.Title) < 0))
            .OrderBy(blog => blog.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(blog => blog.Title)
            .Take(relatedLimit)
            .Select(blog => new BlogCardDto(
                blog.Id,
                blog.Slug,
                blog.Title,
                blog.Excerpt,
                blog.Tags,
                blog.PublicCoverUrl,
                blog.PublishedAt))
            .ToListAsync(cancellationToken);

        var olderRelated = await publishedBlogs
            .Where(blog =>
                (blog.PublishedAt ?? DateTimeOffset.MinValue) < currentPublishedAt
                || ((blog.PublishedAt ?? DateTimeOffset.MinValue) == currentPublishedAt
                    && string.Compare(blog.Title, current.Title) > 0))
            .OrderByDescending(blog => blog.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenBy(blog => blog.Title)
            .Take(relatedLimit)
            .Select(blog => new BlogCardDto(
                blog.Id,
                blog.Slug,
                blog.Title,
                blog.Excerpt,
                blog.Tags,
                blog.PublicCoverUrl,
                blog.PublishedAt))
            .ToListAsync(cancellationToken);

        var related = BuildBalancedRelatedContext(newerRelated, olderRelated, relatedLimit);

        return new BlogDetailContextDto(newer, older, related);
    }

    private static List<BlogCardDto> BuildBalancedRelatedContext(
        List<BlogCardDto> newerClosest,
        List<BlogCardDto> olderClosest,
        int limit)
    {
        if (limit <= 0)
        {
            return [];
        }

        var newerTarget = limit / 2;
        var olderTarget = limit - newerTarget;
        var selectedNewer = newerClosest.Take(newerTarget).ToList();
        var selectedOlder = olderClosest.Take(olderTarget).ToList();

        var remaining = limit - selectedNewer.Count - selectedOlder.Count;
        if (remaining > 0)
        {
            selectedOlder.AddRange(olderClosest.Skip(selectedOlder.Count).Take(remaining));
        }

        remaining = limit - selectedNewer.Count - selectedOlder.Count;
        if (remaining > 0)
        {
            selectedNewer.AddRange(newerClosest.Skip(selectedNewer.Count).Take(remaining));
        }

        selectedNewer.Reverse();
        selectedNewer.AddRange(selectedOlder);
        return selectedNewer;
    }

    private static IQueryable<Blog> ApplySearch(
        IQueryable<Blog> query,
        string? normalizedQuery,
        ContentSearchMode searchMode)
    {
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            return query;
        }

        return searchMode switch
        {
            ContentSearchMode.Title => query.Where(x => x.SearchTitle.Contains(normalizedQuery)),
            ContentSearchMode.Content => query.Where(x => x.SearchText.Contains(normalizedQuery)),
            _ => query.Where(x => x.SearchTitle.Contains(normalizedQuery) || x.SearchText.Contains(normalizedQuery))
        };
    }

    private sealed record BlogContextCurrentRow(
        Guid Id,
        string Title,
        DateTimeOffset? PublishedAt);
}
