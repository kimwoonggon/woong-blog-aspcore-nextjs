using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;
using WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogById;
using WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogs;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Infrastructure.Modules.Content.Blogs.Persistence;

public sealed class BlogQueryStore(WoongBlogDbContext dbContext) : IBlogQueryStore
{
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
            .ToListAsync(cancellationToken);

        var assetIds = blogs
            .Where(x => x.CoverAssetId.HasValue)
            .Select(x => x.CoverAssetId!.Value)
            .Distinct()
            .ToArray();
        var assets = await dbContext.Assets
            .AsNoTracking()
            .Where(x => assetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);

        var items = blogs.Select(blog => new BlogCardDto(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            blog.Tags,
            blog.CoverAssetId is not null && assets.TryGetValue(blog.CoverAssetId.Value, out var coverUrl) ? coverUrl : string.Empty,
            blog.PublishedAt)).ToList();

        return new PagedBlogsDto(items, resolvedPage, pageSize, totalItems, totalPages);
    }

    public async Task<BlogDetailDto?> GetPublishedDetailBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var blog = await dbContext.Blogs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slug == slug && x.Published, cancellationToken);

        if (blog is null)
        {
            return null;
        }

        var coverUrl = blog.CoverAssetId is null
            ? string.Empty
            : await dbContext.Assets
                .AsNoTracking()
                .Where(x => x.Id == blog.CoverAssetId.Value)
                .Select(x => x.PublicUrl)
                .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new BlogDetailDto(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            blog.ContentJson,
            blog.Tags,
            coverUrl,
            blog.PublishedAt);
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
}
