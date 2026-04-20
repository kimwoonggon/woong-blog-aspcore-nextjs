using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Persistence;

public sealed class WorkVideoCommandStore(WoongBlogDbContext dbContext) : IWorkVideoCommandStore
{
    public Task<Work?> GetWorkForUpdateAsync(Guid workId, CancellationToken cancellationToken)
    {
        return dbContext.Works.SingleOrDefaultAsync(x => x.Id == workId, cancellationToken);
    }

    public Task<WorkVideoUploadSession?> GetUploadSessionForUpdateAsync(
        Guid workId,
        Guid uploadSessionId,
        CancellationToken cancellationToken)
    {
        return dbContext.WorkVideoUploadSessions.SingleOrDefaultAsync(
            x => x.Id == uploadSessionId && x.WorkId == workId,
            cancellationToken);
    }

    public Task<WorkVideo?> GetVideoForUpdateAsync(Guid workId, Guid videoId, CancellationToken cancellationToken)
    {
        return dbContext.WorkVideos.SingleOrDefaultAsync(
            x => x.Id == videoId && x.WorkId == workId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<WorkVideo>> GetVideosForWorkAsync(Guid workId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkVideos
            .Where(x => x.WorkId == workId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountVideosAsync(Guid workId, CancellationToken cancellationToken)
    {
        return dbContext.WorkVideos.CountAsync(x => x.WorkId == workId, cancellationToken);
    }

    public async Task<int> GetNextSortOrderAsync(Guid workId, CancellationToken cancellationToken)
    {
        var maxSortOrder = await dbContext.WorkVideos
            .Where(x => x.WorkId == workId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken);

        return (maxSortOrder ?? -1) + 1;
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

    public void AddUploadSession(WorkVideoUploadSession session)
    {
        dbContext.WorkVideoUploadSessions.Add(session);
    }

    public void AddWorkVideo(WorkVideo video)
    {
        dbContext.WorkVideos.Add(video);
    }

    public void RemoveWorkVideo(WorkVideo video)
    {
        dbContext.WorkVideos.Remove(video);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
