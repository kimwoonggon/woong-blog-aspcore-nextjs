using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Infrastructure.Modules.Content.Works.Persistence;

public sealed class WorkVideoQueryStore(
    WoongBlogDbContext dbContext,
    IWorkVideoPlaybackUrlBuilder playbackUrlBuilder) : IWorkVideoQueryStore
{
    public async Task<WorkVideosMutationResult> GetMutationResultAsync(
        Guid workId,
        CancellationToken cancellationToken)
    {
        var work = await dbContext.Works
            .AsNoTracking()
            .SingleAsync(x => x.Id == workId, cancellationToken);

        var videos = await dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => x.WorkId == workId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return new WorkVideosMutationResult(
            work.VideosVersion,
            await BuildVideoDtosAsync(videos, cancellationToken));
    }

    private async Task<List<WorkVideoDto>> BuildVideoDtosAsync(
        IEnumerable<WorkVideo> videos,
        CancellationToken cancellationToken)
    {
        var results = new List<WorkVideoDto>();

        foreach (var video in videos)
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
}
