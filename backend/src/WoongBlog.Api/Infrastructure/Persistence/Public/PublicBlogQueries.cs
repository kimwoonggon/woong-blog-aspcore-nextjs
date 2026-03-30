using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Public.Abstractions;
using WoongBlog.Api.Application.Public.GetBlogBySlug;
using WoongBlog.Api.Application.Public.GetBlogs;
using WoongBlog.Api.Application.Public.GetHome;

namespace WoongBlog.Api.Infrastructure.Persistence.Public;

public sealed class PublicBlogQueries : IPublicBlogQueries
{
    private readonly WoongBlogDbContext _dbContext;

    public PublicBlogQueries(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedBlogsDto> GetBlogsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var assets = await _dbContext.Assets.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
        pageSize = Math.Max(1, pageSize);
        var requestedPage = Math.Max(1, page);

        var query = _dbContext.Blogs.AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var resolvedPage = Math.Min(requestedPage, totalPages);

        var blogs = await query
            .Skip((resolvedPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = blogs.Select(blog => new BlogCardDto(
            blog.Id,
            blog.Slug,
            blog.Title,
            blog.Excerpt,
            blog.Tags,
            blog.CoverAssetId is not null && assets.TryGetValue(blog.CoverAssetId.Value, out var coverUrl) ? coverUrl : string.Empty,
            blog.PublishedAt
        )).ToList();

        return new PagedBlogsDto(items, resolvedPage, pageSize, totalItems, totalPages);
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
