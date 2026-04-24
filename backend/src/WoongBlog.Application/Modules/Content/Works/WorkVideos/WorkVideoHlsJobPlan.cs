using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public sealed record WorkVideoHlsJobPlan(
    Guid WorkId,
    Guid VideoId,
    string HlsPrefix,
    string ManifestStorageKey,
    string TimelinePreviewVttStorageKey,
    string TimelinePreviewSpriteStorageKey,
    string SourceKey,
    string OriginalFileName,
    long FileSize)
{
    public static WorkVideoHlsJobPlan Create(
        Guid workId,
        string storageType,
        string originalFileName,
        long fileSize)
    {
        var videoId = Guid.NewGuid();
        var hlsPrefix = $"videos/{workId:N}/{videoId:N}/hls";
        var manifestStorageKey = $"{hlsPrefix}/{WorkVideoPolicy.HlsManifestFileName}";
        var timelinePreviewVttStorageKey = $"{hlsPrefix}/{WorkVideoPolicy.TimelinePreviewVttFileName}";
        var timelinePreviewSpriteStorageKey = $"{hlsPrefix}/{WorkVideoPolicy.TimelinePreviewSpriteFileName}";

        return new WorkVideoHlsJobPlan(
            workId,
            videoId,
            hlsPrefix,
            manifestStorageKey,
            timelinePreviewVttStorageKey,
            timelinePreviewSpriteStorageKey,
            WorkVideoHlsSourceKey.Create(storageType, manifestStorageKey),
            WorkVideoPolicy.SanitizeOriginalFileName(originalFileName),
            fileSize);
    }

    public WorkVideo ToWorkVideo(int sortOrder, DateTimeOffset createdAt, bool includeTimelinePreview = true)
    {
        return new WorkVideo
        {
            Id = VideoId,
            WorkId = WorkId,
            SourceType = WorkVideoSourceTypes.Hls,
            SourceKey = SourceKey,
            OriginalFileName = OriginalFileName,
            MimeType = WorkVideoPolicy.HlsManifestContentType,
            FileSize = FileSize,
            TimelinePreviewVttStorageKey = includeTimelinePreview ? TimelinePreviewVttStorageKey : null,
            TimelinePreviewSpriteStorageKey = includeTimelinePreview ? TimelinePreviewSpriteStorageKey : null,
            SortOrder = sortOrder,
            CreatedAt = createdAt
        };
    }
}
