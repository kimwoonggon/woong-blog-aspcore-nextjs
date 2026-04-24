using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Infrastructure.Modules.Content.Works.Persistence;

public sealed class WorkVideoCleanupStore(WoongBlogDbContext dbContext) : IWorkVideoCleanupStore
{
    public async Task<IReadOnlyList<WorkVideo>> GetVideosForWorkAsync(Guid workId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkVideos
            .Where(x => x.WorkId == workId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoStorageCleanupJob>> GetPendingCleanupJobsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.VideoStorageCleanupJobs
            .Where(x => x.Status == VideoStorageCleanupJobStatuses.Pending)
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkVideoUploadSession>> GetExpiredUploadSessionsAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        return await dbContext.WorkVideoUploadSessions
            .Where(x => x.ExpiresAt <= utcNow
                && x.Status != WorkVideoUploadSessionStatuses.Confirmed
                && x.Status != WorkVideoUploadSessionStatuses.Expired)
            .ToListAsync(cancellationToken);
    }

    public async Task EnqueueCleanupAsync(
        Guid? workId,
        Guid? workVideoId,
        string sourceType,
        string sourceKey,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (string.Equals(sourceType, WorkVideoSourceTypes.YouTube, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(sourceType, WorkVideoSourceTypes.Hls, StringComparison.OrdinalIgnoreCase))
        {
            if (!WorkVideoHlsSourceKey.TryParse(sourceKey, out sourceType, out sourceKey))
            {
                return;
            }
        }

        var exists = await dbContext.VideoStorageCleanupJobs.AnyAsync(
            x => x.StorageType == sourceType
                && x.StorageKey == sourceKey
                && x.Status == VideoStorageCleanupJobStatuses.Pending,
            cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.VideoStorageCleanupJobs.Add(new VideoStorageCleanupJob
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            WorkVideoId = workVideoId,
            StorageType = sourceType,
            StorageKey = sourceKey,
            AttemptCount = 0,
            Status = VideoStorageCleanupJobStatuses.Pending,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
