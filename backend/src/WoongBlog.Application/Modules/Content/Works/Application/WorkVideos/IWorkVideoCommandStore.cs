using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoCommandStore
{
    Task<Work?> GetWorkForUpdateAsync(Guid workId, CancellationToken cancellationToken);

    Task<WorkVideoUploadSession?> GetUploadSessionForUpdateAsync(
        Guid workId,
        Guid uploadSessionId,
        CancellationToken cancellationToken);

    Task<WorkVideo?> GetVideoForUpdateAsync(Guid workId, Guid videoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkVideo>> GetVideosForWorkAsync(Guid workId, CancellationToken cancellationToken);

    Task<int> CountVideosAsync(Guid workId, CancellationToken cancellationToken);

    Task<int> GetNextSortOrderAsync(Guid workId, CancellationToken cancellationToken);

    Task<IReadOnlyList<VideoStorageCleanupJob>> GetPendingCleanupJobsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkVideoUploadSession>> GetExpiredUploadSessionsAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task EnqueueCleanupAsync(
        Guid? workId,
        Guid? workVideoId,
        string sourceType,
        string sourceKey,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    void AddUploadSession(WorkVideoUploadSession session);

    void AddWorkVideo(WorkVideo video);

    void RemoveWorkVideo(WorkVideo video);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
