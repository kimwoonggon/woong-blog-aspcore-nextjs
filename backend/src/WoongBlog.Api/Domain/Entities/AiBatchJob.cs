namespace WoongBlog.Api.Domain.Entities;

public class AiBatchJob
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string TargetType { get; private set; } = "blog";
    public string Status { get; private set; } = AiBatchJobStates.Queued;
    public string SelectionMode { get; private set; } = "selected";
    public string SelectionLabel { get; private set; } = string.Empty;
    public string SelectionKey { get; private set; } = string.Empty;
    public bool All { get; private set; }
    public bool AutoApply { get; private set; }
    public int? WorkerCount { get; private set; }
    public bool CancelRequested { get; private set; }
    public int TotalCount { get; private set; }
    public int ProcessedCount { get; private set; }
    public int SucceededCount { get; private set; }
    public int FailedCount { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string? ReasoningEffort { get; private set; }
    public string PromptMode { get; private set; } = "blog-fix";
    public Guid? RequestedByProfileId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static AiBatchJob CreateBlogFixJob(string selectionMode, string selectionLabel, string selectionKey, bool all, bool autoApply, int? workerCount, int totalCount, string provider, string model, string? reasoningEffort, DateTimeOffset now)
    {
        return new AiBatchJob
        {
            TargetType = "blog",
            Status = AiBatchJobStates.Queued,
            SelectionMode = selectionMode,
            SelectionLabel = selectionLabel,
            SelectionKey = selectionKey,
            All = all,
            AutoApply = autoApply,
            WorkerCount = workerCount,
            TotalCount = totalCount,
            Provider = provider,
            Model = model,
            ReasoningEffort = reasoningEffort,
            PromptMode = "blog-fix",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkQueued(DateTimeOffset now)
    {
        EnsureActiveState();
        Status = AiBatchJobStates.Queued;
        UpdatedAt = now;
    }

    public void Start(DateTimeOffset now)
    {
        if (Status != AiBatchJobStates.Queued)
        {
            throw new InvalidOperationException("Only queued jobs can start.");
        }

        Status = AiBatchJobStates.Running;
        StartedAt = now;
        UpdatedAt = now;
    }

    public void RequestCancel(DateTimeOffset now)
    {
        EnsureActiveState();
        CancelRequested = true;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureActiveState();
        CancelRequested = true;
        Status = AiBatchJobStates.Cancelled;
        FinishedAt = now;
        UpdatedAt = now;
    }

    public void RefreshCounts(int totalCount, int processedCount, int succeededCount, int failedCount, DateTimeOffset now)
    {
        TotalCount = totalCount;
        ProcessedCount = processedCount;
        SucceededCount = succeededCount;
        FailedCount = failedCount;
        UpdatedAt = now;
    }

    public void Finish(int totalCount, int processedCount, int succeededCount, int failedCount, DateTimeOffset now)
    {
        EnsureActiveState();
        RefreshCounts(totalCount, processedCount, succeededCount, failedCount, now);
        Status = CancelRequested ? AiBatchJobStates.Cancelled :
            failedCount == totalCount && totalCount > 0 ? AiBatchJobStates.Failed :
            AiBatchJobStates.Completed;
        FinishedAt = now;
        UpdatedAt = now;
    }

    private void EnsureActiveState()
    {
        if (Status is AiBatchJobStates.Completed or AiBatchJobStates.Failed or AiBatchJobStates.Cancelled)
        {
            throw new InvalidOperationException("Terminal jobs cannot transition.");
        }
    }
}

public static class AiBatchJobStates
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}
