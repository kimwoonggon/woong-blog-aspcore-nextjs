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
            videos.Select(video => new WorkVideoDto(
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
                ResolveTimelinePreviewUrl(video, video.TimelinePreviewVttStorageKey),
                ResolveTimelinePreviewUrl(video, video.TimelinePreviewSpriteStorageKey),
                video.SortOrder,
                video.CreatedAt)).ToList());
    }

    private string? ResolveTimelinePreviewUrl(WorkVideo video, string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        if (string.Equals(video.SourceType, WorkVideoSourceTypes.Hls, StringComparison.OrdinalIgnoreCase)
            && WorkVideoHlsSourceKey.TryParse(video.SourceKey, out var storageType, out _))
        {
            return playbackUrlBuilder.BuildStorageObjectUrl(storageType, storageKey);
        }

        return playbackUrlBuilder.BuildStorageObjectUrl(video.SourceType, storageKey);
    }
}
