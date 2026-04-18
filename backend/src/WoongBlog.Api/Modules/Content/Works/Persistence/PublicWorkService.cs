using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Composition.Application.GetHome;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorkBySlug;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorks;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Persistence;

public sealed class PublicWorkService : IPublicWorkService
{
    private readonly WoongBlogDbContext _dbContext;
    private readonly IWorkVideoPlaybackUrlBuilder _playbackUrlBuilder;

    public PublicWorkService(WoongBlogDbContext dbContext, IWorkVideoPlaybackUrlBuilder playbackUrlBuilder)
    {
        _dbContext = dbContext;
        _playbackUrlBuilder = playbackUrlBuilder;
    }

    public async Task<PagedWorksDto> GetWorksAsync(GetWorksQuery queryInput, CancellationToken cancellationToken)
    {
        var assets = await _dbContext.Assets.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
        var pageSize = Math.Max(1, queryInput.PageSize);
        var requestedPage = Math.Max(1, queryInput.Page);

        var query = _dbContext.Works.AsNoTracking()
            .Where(x => x.Published)
            .OrderByDescending(x => x.PublishedAt);

        var normalizedSearch = queryInput.Query?.Trim();
        List<Work> works;
        int totalItems;
        int totalPages;
        int page;
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            totalItems = await query.CountAsync(cancellationToken);
            totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            page = Math.Min(requestedPage, totalPages);

            works = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var searchMode = queryInput.SearchMode.Trim().ToLowerInvariant();
            var filteredWorks = (await query.ToListAsync(cancellationToken))
                .Where(work => searchMode == "content"
                    ? ContentSearchText.AnyContainsNormalized(normalizedSearch, work.Excerpt, work.ContentJson)
                    : ContentSearchText.ContainsNormalized(work.Title, normalizedSearch))
                .ToList();

            totalItems = filteredWorks.Count;
            totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            page = Math.Min(requestedPage, totalPages);

            works = filteredWorks
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        var workIds = works.Select(work => work.Id).ToArray();
        var videoRows = await _dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => workIds.Contains(x.WorkId))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var videoLookup = videoRows
            .GroupBy(x => x.WorkId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<WorkVideo>)x.ToList());

        var items = works.Select(work => new WorkCardDto(
            work.Id,
            work.Slug,
            work.Title,
            work.Excerpt,
            work.Category,
            work.Period,
            work.Tags,
            WorkThumbnailUrlResolver.ResolveThumbnailUrl(
                work.ThumbnailAssetId,
                work.ContentJson,
                videoLookup.TryGetValue(work.Id, out var workVideos) ? workVideos : Array.Empty<WorkVideo>(),
                assets),
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
        var videoRows = await _dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => x.WorkId == work.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var videos = videoRows.Select(x => new WorkVideoDto(
            x.Id,
            x.SourceType,
            x.SourceKey,
            _playbackUrlBuilder.BuildPlaybackUrl(x.SourceType, x.SourceKey),
            x.OriginalFileName,
            x.MimeType,
            x.FileSize,
            x.SortOrder,
            x.CreatedAt
        )).ToList();

        var thumbnailUrl = WorkThumbnailUrlResolver.ResolveThumbnailUrl(
            work.ThumbnailAssetId,
            work.ContentJson,
            videoRows,
            assets);

        return new WorkDetailDto(
            work.Id,
            work.Slug,
            work.Title,
            work.Excerpt,
            work.ContentJson,
            work.Category,
            work.Period,
            work.Tags,
            thumbnailUrl,
            work.IconAssetId is not null && assets.TryGetValue(work.IconAssetId.Value, out var iconUrl) ? iconUrl : string.Empty,
            work.PublishedAt,
            work.VideosVersion,
            videos
        );
    }
}
