using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoCleanupStore
{
    Task<IReadOnlyList<WorkVideo>> GetVideosForWorkAsync(Guid workId, CancellationToken cancellationToken);

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

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
