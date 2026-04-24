using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Application.Modules.AI.BatchJobs;

public static class AiBatchJobProgressPolicy
{
    public static void MarkQueued(AiBatchJob job, DateTimeOffset timestamp)
    {
        job.Status = AiBatchJobStates.Queued;
        job.UpdatedAt = timestamp;
    }

    public static void MarkRunning(AiBatchJob job, DateTimeOffset timestamp)
    {
        job.Status = AiBatchJobStates.Running;
        job.StartedAt = timestamp;
        job.UpdatedAt = timestamp;
    }

    public static void MarkRunning(AiBatchJobItem item, DateTimeOffset timestamp)
    {
        item.Status = AiBatchJobItemStates.Running;
        item.StartedAt = timestamp;
    }

    public static void MarkCancelled(AiBatchJobItem item, DateTimeOffset timestamp)
    {
        item.Status = AiBatchJobItemStates.Cancelled;
        item.FinishedAt = timestamp;
    }

    public static void MarkCancelled(AiBatchJob job, DateTimeOffset timestamp)
    {
        job.Status = AiBatchJobStates.Cancelled;
        job.FinishedAt = timestamp;
        job.UpdatedAt = timestamp;
    }

    public static void MarkFailed(AiBatchJobItem item, string error, DateTimeOffset timestamp)
    {
        item.Status = AiBatchJobItemStates.Failed;
        item.Error = error;
        item.FinishedAt = timestamp;
    }

    public static void RefreshCounts(AiBatchJob job, IReadOnlyList<AiBatchJobItem> items, DateTimeOffset timestamp)
    {
        job.TotalCount = items.Count;
        job.ProcessedCount = CountProcessed(items);
        job.SucceededCount = CountSucceeded(items);
        job.FailedCount = CountFailed(items);
        job.UpdatedAt = timestamp;
    }

    public static void Finalize(AiBatchJob job, IReadOnlyList<AiBatchJobItem> items, DateTimeOffset timestamp)
    {
        RefreshCounts(job, items, timestamp);
        job.Status = job.CancelRequested
            ? AiBatchJobStates.Cancelled
            : job.FailedCount == job.TotalCount && job.TotalCount > 0
                ? AiBatchJobStates.Failed
                : AiBatchJobStates.Completed;
        job.FinishedAt = timestamp;
    }

    private static int CountProcessed(IReadOnlyList<AiBatchJobItem> items) =>
        items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Failed or AiBatchJobItemStates.Applied or AiBatchJobItemStates.Cancelled);

    private static int CountSucceeded(IReadOnlyList<AiBatchJobItem> items) =>
        items.Count(item => item.Status is AiBatchJobItemStates.Succeeded or AiBatchJobItemStates.Applied);

    private static int CountFailed(IReadOnlyList<AiBatchJobItem> items) =>
        items.Count(item => item.Status == AiBatchJobItemStates.Failed);
}
