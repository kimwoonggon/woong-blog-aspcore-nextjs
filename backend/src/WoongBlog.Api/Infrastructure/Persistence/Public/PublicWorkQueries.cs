using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Public.Abstractions;
using WoongBlog.Api.Application.Public.GetHome;
using WoongBlog.Api.Application.Public.GetWorkBySlug;
using WoongBlog.Api.Application.Public.GetWorks;
using WoongBlog.Api.Infrastructure.Persistence.Assets;

namespace WoongBlog.Api.Infrastructure.Persistence.Public;

public sealed class PublicWorkQueries : IPublicWorkQueries
{
    private readonly WoongBlogDbContext _dbContext;

    public PublicWorkQueries(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedWorksDto> GetWorksAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        pageSize = Math.Max(1, pageSize);
        var requestedPage = Math.Max(1, page);

        var query = _dbContext.Works.AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var resolvedPage = Math.Min(requestedPage, totalPages);

        var works = await query
            .Skip((resolvedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Slug,
                x.Title,
                x.Excerpt,
                x.Category,
                x.Period,
                x.Tags,
                x.ThumbnailAssetId,
                x.IconAssetId,
                x.PublishedAt
            })
            .ToListAsync(cancellationToken);

        var assets = await _dbContext.LoadPublicUrlLookupAsync(
            works.SelectMany(work => new Guid?[] { work.ThumbnailAssetId, work.IconAssetId }),
            cancellationToken);

        var items = works.Select(work => new WorkCardDto(
            work.Id,
            work.Slug,
            work.Title,
            work.Excerpt,
            work.Category,
            work.Period,
            work.Tags,
            work.ThumbnailAssetId is not null && assets.TryGetValue(work.ThumbnailAssetId.Value, out var thumbnailUrl) ? thumbnailUrl : string.Empty,
            work.IconAssetId is not null && assets.TryGetValue(work.IconAssetId.Value, out var iconUrl) ? iconUrl : string.Empty,
            work.PublishedAt
        )).ToList();

        return new PagedWorksDto(items, resolvedPage, pageSize, totalItems, totalPages);
    }

    public async Task<WorkDetailDto?> GetWorkBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.AsNoTracking()
            .Where(x => x.Slug == slug && x.Published)
            .Select(x => new
            {
                x.Id,
                x.Slug,
                x.Title,
                x.Excerpt,
                x.ContentJson,
                x.Category,
                x.Period,
                x.Tags,
                x.ThumbnailAssetId,
                x.IconAssetId,
                x.PublishedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (work is null)
        {
            return null;
        }

        var assets = await _dbContext.LoadPublicUrlLookupAsync(
            [work.ThumbnailAssetId, work.IconAssetId],
            cancellationToken);

        return new WorkDetailDto(
            work.Id,
            work.Slug,
            work.Title,
            work.Excerpt,
            work.ContentJson,
            work.Category,
            work.Period,
            work.Tags,
            work.ThumbnailAssetId is not null && assets.TryGetValue(work.ThumbnailAssetId.Value, out var thumbnailUrl) ? thumbnailUrl : string.Empty,
            work.IconAssetId is not null && assets.TryGetValue(work.IconAssetId.Value, out var iconUrl) ? iconUrl : string.Empty,
            work.PublishedAt
        );
    }
}
