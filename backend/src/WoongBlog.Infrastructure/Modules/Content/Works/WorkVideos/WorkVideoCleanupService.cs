using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed class WorkVideoService(
    IWorkVideoCleanupStore cleanupStore,
    IEnumerable<IVideoObjectStorage> storages) : IWorkVideoCleanupService
{
    private readonly IWorkVideoCleanupStore _cleanupStore = cleanupStore;
    private readonly IReadOnlyDictionary<string, IVideoObjectStorage> _storages = storages
        .ToDictionary(storage => storage.StorageType, StringComparer.OrdinalIgnoreCase);

    public async Task EnqueueCleanupForWorkAsync(Guid workId, CancellationToken cancellationToken)
    {
        var videos = await _cleanupStore.GetVideosForWorkAsync(workId, cancellationToken);

        foreach (var video in videos)
        {
            await EnqueueCleanupAsync(workId, video.Id, video.SourceType, video.SourceKey, cancellationToken);
        }

        await _cleanupStore.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ProcessCleanupJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await _cleanupStore.GetPendingCleanupJobsAsync(cancellationToken);

        foreach (var job in jobs)
        {
            if (!_storages.TryGetValue(job.StorageType, out var storage))
            {
                job.Status = VideoStorageCleanupJobStatuses.Failed;
                job.LastError = "Storage backend not available.";
                job.UpdatedAt = DateTimeOffset.UtcNow;
                continue;
            }

            try
            {
                await storage.DeleteAsync(job.StorageKey, cancellationToken);
                job.Status = VideoStorageCleanupJobStatuses.Succeeded;
                job.LastError = null;
            }
            catch (Exception exception)
            {
                job.AttemptCount += 1;
                job.LastError = exception.Message;
                job.Status = job.AttemptCount >= 5
                    ? VideoStorageCleanupJobStatuses.Failed
                    : VideoStorageCleanupJobStatuses.Pending;
            }

            job.UpdatedAt = DateTimeOffset.UtcNow;
        }

        if (jobs.Count > 0)
        {
            await _cleanupStore.SaveChangesAsync(cancellationToken);
        }

        return jobs.Count;
    }

    public async Task<int> ExpireUploadSessionsAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sessions = await _cleanupStore.GetExpiredUploadSessionsAsync(utcNow, cancellationToken);

        foreach (var session in sessions)
        {
            await EnqueueCleanupAsync(session.WorkId, null, session.StorageType, session.StorageKey, cancellationToken);
            session.Status = WorkVideoUploadSessionStatuses.Expired;
        }

        if (sessions.Count > 0)
        {
            await _cleanupStore.SaveChangesAsync(cancellationToken);
        }

        return sessions.Count;
    }

    private async Task EnqueueCleanupAsync(
        Guid? workId,
        Guid? workVideoId,
        string sourceType,
        string sourceKey,
        CancellationToken cancellationToken)
    {
        await _cleanupStore.EnqueueCleanupAsync(
            workId,
            workVideoId,
            sourceType,
            sourceKey,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }
}
