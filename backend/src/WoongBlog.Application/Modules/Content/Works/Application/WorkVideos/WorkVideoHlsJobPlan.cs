using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed record WorkVideoHlsJobPlan(
    Guid WorkId,
    Guid VideoId,
    string HlsPrefix,
    string ManifestStorageKey,
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

        return new WorkVideoHlsJobPlan(
            workId,
            videoId,
            hlsPrefix,
            manifestStorageKey,
            WorkVideoHlsSourceKey.Create(storageType, manifestStorageKey),
            WorkVideoPolicy.SanitizeOriginalFileName(originalFileName),
            fileSize);
    }

    public WorkVideo ToWorkVideo(int sortOrder, DateTimeOffset createdAt)
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
            SortOrder = sortOrder,
            CreatedAt = createdAt
        };
    }
}
