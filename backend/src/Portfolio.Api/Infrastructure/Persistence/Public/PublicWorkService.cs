using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Application.Public.Abstractions;
using Portfolio.Api.Application.Public.GetHome;
using Portfolio.Api.Application.Public.GetWorkBySlug;
using Portfolio.Api.Application.Public.GetWorks;

namespace Portfolio.Api.Infrastructure.Persistence.Public;

public sealed class PublicWorkService : IPublicWorkService
{
    private readonly PortfolioDbContext _dbContext;

    public PublicWorkService(PortfolioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedWorksDto> GetWorksAsync(GetWorksQuery queryInput, CancellationToken cancellationToken)
    {
        var assets = await _dbContext.Assets.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
        var pageSize = Math.Max(1, queryInput.PageSize);
        var requestedPage = Math.Max(1, queryInput.Page);

        var query = _dbContext.Works.AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var page = Math.Min(requestedPage, totalPages);

        var works = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

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

        return new PagedWorksDto(items, page, pageSize, totalItems, totalPages);
    }

    public async Task<WorkDetailDto?> GetWorkBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slug == slug && x.Published, cancellationToken);

        if (work is null)
        {
            return null;
        }

        var assets = await _dbContext.Assets.AsNoTracking()
            .Where(x => x.Id == work.ThumbnailAssetId || x.Id == work.IconAssetId)
            .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);

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
