using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.AI.Application.Abstractions;

public sealed record AiBlogTarget(Guid Id, string Title);

public sealed record AiBatchJobCounts(
    int RunningCount,
    int QueuedCount,
    int CompletedCount,
    int FailedCount,
    int CancelledCount);

public interface IAiBlogFixBatchStore
{
    Task<IReadOnlyList<Blog>> GetBlogsForFixAsync(
        bool all,
        IReadOnlyCollection<Guid>? blogIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AiBlogTarget>> GetBlogTargetsAsync(
        bool all,
        IReadOnlyCollection<Guid>? blogIds,
        CancellationToken cancellationToken);

    Task<AiBatchJob?> GetActiveBlogJobBySelectionKeyAsync(string selectionKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiBatchJob>> GetRunningBlogJobsAsync(CancellationToken cancellationToken);
    Task<AiBatchJob?> GetBlogJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<AiBatchJobItem?> GetJobItemAsync(Guid itemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiBatchJob>> GetRecentBlogJobsAsync(int take, CancellationToken cancellationToken);
    Task<AiBatchJobCounts> GetBlogJobCountsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AiBatchJobItem>> GetJobItemsAsync(Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiBatchJobItem>> GetSucceededUnappliedItemsAsync(Guid jobId, IReadOnlyCollection<Guid>? jobItemIds, CancellationToken cancellationToken);
    Task<Dictionary<Guid, Blog>> GetBlogsForUpdateAsync(IReadOnlyCollection<Guid> blogIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiBatchJob>> GetQueuedBlogJobsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AiBatchJob>> GetCompletedBlogJobsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AiBatchJobItem>> GetPendingItemsForJobsAsync(IReadOnlyCollection<Guid> jobIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiBatchJobItem>> GetItemsForJobsAsync(IReadOnlyCollection<Guid> jobIds, CancellationToken cancellationToken);
    void AddJob(AiBatchJob job, IReadOnlyList<AiBatchJobItem> items);
    void RemoveJobs(IReadOnlyList<AiBatchJob> jobs);
    void RemoveItems(IReadOnlyList<AiBatchJobItem> items);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
