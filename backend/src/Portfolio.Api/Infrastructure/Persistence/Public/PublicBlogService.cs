using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Application.Public.Abstractions;
using Portfolio.Api.Application.Public.GetBlogBySlug;
using Portfolio.Api.Application.Public.GetBlogs;
using Portfolio.Api.Application.Public.GetHome;

namespace Portfolio.Api.Infrastructure.Persistence.Public;

public sealed class PublicBlogService : IPublicBlogService
{
    private readonly PortfolioDbContext _dbContext;

    public PublicBlogService(PortfolioDbContext dbContext)
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

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var page = Math.Min(requestedPage, totalPages);

        var blogs = await query
            .Skip((page - 1) * pageSize)
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
