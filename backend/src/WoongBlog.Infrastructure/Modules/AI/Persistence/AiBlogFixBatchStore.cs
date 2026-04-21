using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.AI.Application.Abstractions;

namespace WoongBlog.Api.Modules.AI.Persistence;

public sealed class AiBlogFixBatchStore(WoongBlogDbContext dbContext) :
    IAiBatchTargetQueryStore,
    IAiBatchJobQueryStore,
    IAiBatchJobCommandStore
{
    public async Task<IReadOnlyList<Blog>> GetBlogsForFixAsync(
        bool all,
        IReadOnlyCollection<Guid>? blogIds,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Blogs.AsQueryable();
        if (!all && blogIds is { Count: > 0 })
        {
            query = query.Where(blog => blogIds.Contains(blog.Id));
        }

        return await query
            .OrderByDescending(blog => blog.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiBlogTarget>> GetBlogTargetsAsync(
        bool all,
        IReadOnlyCollection<Guid>? blogIds,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Blogs.AsQueryable();
        if (!all && blogIds is { Count: > 0 })
        {
            query = query.Where(blog => blogIds.Contains(blog.Id));
        }

        return await query
            .OrderByDescending(blog => blog.UpdatedAt)
            .Select(blog => new AiBlogTarget(blog.Id, blog.Title))
            .ToListAsync(cancellationToken);
    }

    public Task<AiBatchJob?> GetActiveBlogJobBySelectionKeyAsync(string selectionKey, CancellationToken cancellationToken)
    {
        return dbContext.AiBatchJobs
            .Where(job =>
                job.TargetType == "blog"
                && job.SelectionKey == selectionKey
                && (job.Status == AiBatchJobStates.Queued || job.Status == AiBatchJobStates.Running))
            .OrderByDescending(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiBatchJob>> GetRunningBlogJobsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog" && job.Status == AiBatchJobStates.Running)
            .OrderBy(job => job.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<AiBatchJob?> GetBlogJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return dbContext.AiBatchJobs.SingleOrDefaultAsync(x => x.Id == jobId && x.TargetType == "blog", cancellationToken);
    }

    public Task<AiBatchJobItem?> GetJobItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        return dbContext.AiBatchJobItems.SingleOrDefaultAsync(x => x.Id == itemId, cancellationToken);
    }

    public async Task<IReadOnlyList<AiBatchJob>> GetRecentBlogJobsAsync(int take, CancellationToken cancellationToken)
    {
        return await dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog")
            .OrderByDescending(job => job.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<AiBatchJobCounts> GetBlogJobCountsAsync(CancellationToken cancellationToken)
    {
        var counts = await dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog")
            .GroupBy(job => job.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return new AiBatchJobCounts(
            RunningCount: counts.Where(x => x.Status == AiBatchJobStates.Running).Select(x => x.Count).SingleOrDefault(),
            QueuedCount: counts.Where(x => x.Status == AiBatchJobStates.Queued).Select(x => x.Count).SingleOrDefault(),
            CompletedCount: counts.Where(x => x.Status == AiBatchJobStates.Completed).Select(x => x.Count).SingleOrDefault(),
            FailedCount: counts.Where(x => x.Status == AiBatchJobStates.Failed).Select(x => x.Count).SingleOrDefault(),
            CancelledCount: counts.Where(x => x.Status == AiBatchJobStates.Cancelled).Select(x => x.Count).SingleOrDefault());
    }

    public async Task<IReadOnlyList<AiBatchJobItem>> GetJobItemsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiBatchJobItem>> GetSucceededUnappliedItemsAsync(
        Guid jobId,
        IReadOnlyCollection<Guid>? jobItemIds,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AiBatchJobItems
            .Where(item => item.JobId == jobId && item.Status == AiBatchJobItemStates.Succeeded && item.AppliedAt == null);

        if (jobItemIds is { Count: > 0 })
        {
            query = query.Where(item => jobItemIds.Contains(item.Id));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<Dictionary<Guid, Blog>> GetBlogsForUpdateAsync(IReadOnlyCollection<Guid> blogIds, CancellationToken cancellationToken)
    {
        return dbContext.Blogs
            .Where(blog => blogIds.Contains(blog.Id))
            .ToDictionaryAsync(blog => blog.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<AiBatchJob>> GetQueuedBlogJobsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog" && job.Status == AiBatchJobStates.Queued)
            .OrderBy(job => job.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiBatchJob>> GetCompletedBlogJobsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AiBatchJobs
            .Where(job => job.TargetType == "blog" && job.Status == AiBatchJobStates.Completed)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiBatchJobItem>> GetPendingItemsForJobsAsync(IReadOnlyCollection<Guid> jobIds, CancellationToken cancellationToken)
    {
        return await dbContext.AiBatchJobItems
            .Where(item => jobIds.Contains(item.JobId) && item.Status == AiBatchJobItemStates.Pending)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiBatchJobItem>> GetItemsForJobsAsync(IReadOnlyCollection<Guid> jobIds, CancellationToken cancellationToken)
    {
        return await dbContext.AiBatchJobItems
            .Where(item => jobIds.Contains(item.JobId))
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void AddJob(AiBatchJob job, IReadOnlyList<AiBatchJobItem> items)
    {
        dbContext.AiBatchJobs.Add(job);
        dbContext.AiBatchJobItems.AddRange(items);
    }

    public void RemoveJobs(IReadOnlyList<AiBatchJob> jobs)
    {
        dbContext.AiBatchJobs.RemoveRange(jobs);
    }

    public void RemoveItems(IReadOnlyList<AiBatchJobItem> items)
    {
        dbContext.AiBatchJobItems.RemoveRange(items);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
