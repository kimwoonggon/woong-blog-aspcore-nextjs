using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Works.Abstractions;
using WoongBlog.Application.Modules.Content.Works.GetAdminWorkById;
using WoongBlog.Application.Modules.Content.Works.GetAdminWorks;
using WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;
using WoongBlog.Application.Modules.Content.Works.GetWorks;
using WoongBlog.Application.Modules.Content.Works.Support;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Infrastructure.Modules.Content.Works.Persistence;

public sealed class WorkQueryStore(
    WoongBlogDbContext dbContext,
    IWorkVideoPlaybackUrlBuilder playbackUrlBuilder) : IWorkQueryStore
{
    private const string SocialShareMessagePropertyName = "socialShareMessage";

    public async Task<IReadOnlyList<AdminWorkListItemDto>> GetAdminListAsync(CancellationToken cancellationToken)
    {
        var works = await dbContext.Works
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var workIds = works.Select(x => x.Id).ToArray();
        var assetIds = works
            .Where(x => x.ThumbnailAssetId.HasValue)
            .Select(x => x.ThumbnailAssetId!.Value)
            .Distinct()
            .ToArray();
        var assetLookup = await dbContext.Assets
            .AsNoTracking()
            .Where(x => assetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
        var workVideos = await GetVideoLookupAsync(workIds, cancellationToken);

        return works
            .Select(x => new AdminWorkListItemDto(
                x.Id,
                x.Title,
                x.Slug,
                x.Excerpt,
                x.Category,
                x.Period,
                x.Tags,
                WorkThumbnailUrlResolver.ResolveThumbnailUrl(
                    x.ThumbnailAssetId,
                    x.ContentJson,
                    workVideos.TryGetValue(x.Id, out var videos) ? videos : Array.Empty<WorkVideo>(),
                    assetLookup),
                x.Published,
                x.PublishedAt,
                x.CreatedAt,
                x.UpdatedAt))
            .ToList();
    }

    public async Task<AdminWorkDetailDto?> GetAdminDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var work = await dbContext.Works
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return work is null
            ? null
            : await BuildAdminDetailAsync(work, cancellationToken);
    }

    public async Task<PagedWorksDto> GetPublishedPageAsync(
        int page,
        int pageSize,
        string? normalizedQuery,
        ContentSearchMode searchMode,
        CancellationToken cancellationToken)
    {
        var query = ApplySearch(
                dbContext.Works.AsNoTracking().Where(x => x.Published),
                normalizedQuery,
                searchMode)
            .OrderByDescending(x => x.PublishedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var resolvedPage = Math.Min(page, totalPages);

        var works = await query
            .Skip((resolvedPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var workIds = works.Select(work => work.Id).ToArray();
        var workVideos = await GetVideoLookupAsync(workIds, cancellationToken);
        var assetIds = works
            .SelectMany(work => new[] { work.ThumbnailAssetId, work.IconAssetId })
            .Where(assetId => assetId.HasValue)
            .Select(assetId => assetId!.Value)
            .Distinct()
            .ToArray();
        var assets = await dbContext.Assets.AsNoTracking()
            .Where(x => assetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);

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
                workVideos.TryGetValue(work.Id, out var videos) ? videos : Array.Empty<WorkVideo>(),
                assets),
            ResolveAssetUrl(work.IconAssetId, assets),
            work.PublishedAt)).ToList();

        return new PagedWorksDto(items, resolvedPage, pageSize, totalItems, totalPages);
    }

    public async Task<WorkDetailDto?> GetPublishedDetailBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var work = await dbContext.Works.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slug == slug && x.Published, cancellationToken);

        return work is null
            ? null
            : await BuildPublicDetailAsync(work, cancellationToken);
    }

    private async Task<AdminWorkDetailDto> BuildAdminDetailAsync(Work work, CancellationToken cancellationToken)
    {
        var assetLookup = await GetAssetLookupAsync(work.ThumbnailAssetId, work.IconAssetId, cancellationToken);
        var videoRows = await GetVideosAsync(work.Id, cancellationToken);
        var videos = await BuildVideoDtosAsync(videoRows, cancellationToken);
        var thumbnailUrl = WorkThumbnailUrlResolver.ResolveThumbnailUrl(
            work.ThumbnailAssetId,
            work.ContentJson,
            videoRows,
            assetLookup);

        return new AdminWorkDetailDto(
            work.Id,
            work.Title,
            work.Slug,
            work.Excerpt,
            work.Category,
            work.Period,
            work.Tags,
            work.Published,
            work.PublishedAt,
            work.UpdatedAt,
            work.VideosVersion,
            AdminContentJson.ParseObject(work.AllPropertiesJson),
            new AdminWorkContentDto(AdminContentJson.ExtractHtml(work.ContentJson)),
            work.ThumbnailAssetId,
            work.IconAssetId,
            thumbnailUrl,
            ResolveAssetUrl(work.IconAssetId, assetLookup),
            videos);
    }

    private async Task<WorkDetailDto> BuildPublicDetailAsync(Work work, CancellationToken cancellationToken)
    {
        var assets = await GetAssetLookupAsync(work.ThumbnailAssetId, work.IconAssetId, cancellationToken);
        var videoRows = await GetVideosAsync(work.Id, cancellationToken);
        var videos = await BuildVideoDtosAsync(videoRows, cancellationToken);
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
            ResolveAssetUrl(work.IconAssetId, assets),
            work.PublishedAt,
            ResolveSocialShareMessage(work.AllPropertiesJson),
            work.VideosVersion,
            videos);
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<WorkVideo>>> GetVideoLookupAsync(
        IReadOnlyCollection<Guid> workIds,
        CancellationToken cancellationToken)
    {
        var videoRows = await dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => workIds.Contains(x.WorkId))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return videoRows
            .GroupBy(x => x.WorkId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<WorkVideo>)x.ToList());
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetAssetLookupAsync(
        Guid? thumbnailAssetId,
        Guid? iconAssetId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Assets.AsNoTracking()
            .Where(x => x.Id == thumbnailAssetId || x.Id == iconAssetId)
            .ToDictionaryAsync(x => x.Id, x => x.PublicUrl, cancellationToken);
    }

    private async Task<IReadOnlyList<WorkVideo>> GetVideosAsync(Guid workId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => x.WorkId == workId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<WorkVideoDto>> BuildVideoDtosAsync(
        IEnumerable<WorkVideo> videoRows,
        CancellationToken cancellationToken)
    {
        var results = new List<WorkVideoDto>();

        foreach (var video in videoRows)
        {
            var (previewStorageType, hasPreviewStorageType) = ResolvePreviewStorageType(video);
            var hasPreviewAssets = hasPreviewStorageType
                && !string.IsNullOrWhiteSpace(video.TimelinePreviewVttStorageKey)
                && !string.IsNullOrWhiteSpace(video.TimelinePreviewSpriteStorageKey)
                && await playbackUrlBuilder.StorageObjectExistsAsync(previewStorageType, video.TimelinePreviewVttStorageKey!, cancellationToken)
                && await playbackUrlBuilder.StorageObjectExistsAsync(previewStorageType, video.TimelinePreviewSpriteStorageKey!, cancellationToken);

            results.Add(new WorkVideoDto(
                video.Id,
                video.SourceType,
                video.SourceKey,
                playbackUrlBuilder.BuildPlaybackUrl(video.SourceType, video.SourceKey),
                video.OriginalFileName,
                video.MimeType,
                video.FileSize,
                video.Width,
                video.Height,
                video.DurationSeconds,
                hasPreviewAssets ? playbackUrlBuilder.BuildStorageObjectUrl(previewStorageType, video.TimelinePreviewVttStorageKey!) : null,
                hasPreviewAssets ? playbackUrlBuilder.BuildStorageObjectUrl(previewStorageType, video.TimelinePreviewSpriteStorageKey!) : null,
                video.SortOrder,
                video.CreatedAt));
        }

        return results;
    }

    private static (string StorageType, bool IsResolved) ResolvePreviewStorageType(WorkVideo video)
    {
        if (string.Equals(video.SourceType, WorkVideoSourceTypes.Hls, StringComparison.OrdinalIgnoreCase)
            && WorkVideoHlsSourceKey.TryParse(video.SourceKey, out var storageType, out _))
        {
            return (storageType, true);
        }

        return string.IsNullOrWhiteSpace(video.SourceType)
            ? (string.Empty, false)
            : (video.SourceType, true);
    }

    private static IQueryable<Work> ApplySearch(
        IQueryable<Work> query,
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

    private static string ResolveAssetUrl(Guid? assetId, IReadOnlyDictionary<Guid, string> assets)
    {
        return assetId is not null && assets.TryGetValue(assetId.Value, out var url)
            ? url
            : string.Empty;
    }

    private static string? ResolveSocialShareMessage(string allPropertiesJson)
    {
        try
        {
            using var document = JsonDocument.Parse(allPropertiesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!document.RootElement.TryGetProperty(SocialShareMessagePropertyName, out var propertyValue))
            {
                return null;
            }

            var message = propertyValue.ValueKind switch
            {
                JsonValueKind.String => propertyValue.GetString(),
                JsonValueKind.Null => null,
                _ => propertyValue.ToString()
            };

            return string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        }
        catch
        {
            return null;
        }
    }
}
