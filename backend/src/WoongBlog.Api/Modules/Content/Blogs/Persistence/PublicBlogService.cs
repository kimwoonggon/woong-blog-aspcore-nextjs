using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Composition.Application.GetHome;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogBySlug;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Blogs.Persistence;

public sealed class PublicBlogService : IPublicBlogService
{
    private readonly WoongBlogDbContext _dbContext;

    public PublicBlogService(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedBlogsDto> GetBlogsAsync(GetBlogsQuery queryInput, CancellationToken cancellationToken)
    {
        var assets = await _dbContext.Assets.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
        var pageSize = Math.Max(1, queryInput.PageSize);
        var requestedPage = Math.Max(1, queryInput.Page);

        var query = _dbContext.Blogs.AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt);

        var normalizedSearch = queryInput.Query?.Trim();
        List<Blog> blogs;
        int totalItems;
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            totalItems = await query.CountAsync(cancellationToken);
            var totalPagesForUnfiltered = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            var pageForUnfiltered = Math.Min(requestedPage, totalPagesForUnfiltered);

            blogs = await query
                .Skip((pageForUnfiltered - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return BuildPagedBlogs(blogs, assets, pageForUnfiltered, pageSize, totalItems, totalPagesForUnfiltered);
        }

        var searchMode = queryInput.SearchMode.Trim().ToLowerInvariant();
        var filteredBlogs = (await query.ToListAsync(cancellationToken))
            .Where(blog => searchMode == "content"
                ? ContentSearchText.AnyContainsNormalized(normalizedSearch, blog.Excerpt, blog.ContentJson)
                : ContentSearchText.ContainsNormalized(blog.Title, normalizedSearch))
            .ToList();

        totalItems = filteredBlogs.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var page = Math.Min(requestedPage, totalPages);

        blogs = filteredBlogs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return BuildPagedBlogs(blogs, assets, page, pageSize, totalItems, totalPages);
    }

    private static PagedBlogsDto BuildPagedBlogs(
        IEnumerable<Blog> blogs,
        IReadOnlyDictionary<Guid, string> assets,
        int page,
        int pageSize,
        int totalItems,
        int totalPages)
    {
        var items = blogs.Select(blog => new BlogCardDto(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            blog.Tags,
            blog.CoverAssetId is not null && assets.TryGetValue(blog.CoverAssetId.Value, out var coverUrl) ? coverUrl : string.Empty,
            blog.PublishedAt
        )).ToList();

        return new PagedBlogsDto(items, page, pageSize, totalItems, totalPages);
    }

    public async Task<BlogDetailDto?> GetBlogBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var blog = await _dbContext.Blogs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slug == slug && x.Published, cancellationToken);

        if (blog is null)
        {
            return null;
        }

        var coverUrl = blog.CoverAssetId is null
            ? string.Empty
            : await _dbContext.Assets
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
            blog.PublishedAt
        );
    }
}
