namespace WoongBlog.Api.Domain.Entities;

public class AiBatchJobItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid JobId { get; private set; }
    public Guid EntityId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Status { get; private set; } = AiBatchJobItemStates.Pending;
    public string? FixedHtml { get; private set; }
    public string? Error { get; private set; }
    public string? Provider { get; private set; }
    public string? Model { get; private set; }
    public string? ReasoningEffort { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset? AppliedAt { get; private set; }

    public static AiBatchJobItem Create(Guid jobId, Guid entityId, string title, DateTimeOffset createdAt)
    {
        return new AiBatchJobItem
        {
            JobId = jobId,
            EntityId = entityId,
            Title = title,
            Status = AiBatchJobItemStates.Pending,
            CreatedAt = createdAt
        };
    }

    public void Start(DateTimeOffset now)
    {
        if (Status != AiBatchJobItemStates.Pending)
        {
            throw new InvalidOperationException("Only pending items can start.");
        }

        Status = AiBatchJobItemStates.Running;
        StartedAt = now;
    }

    public void RecordSuccess(string fixedHtml, string provider, string model, string? reasoningEffort)
    {
        EnsureNotTerminal();
        FixedHtml = fixedHtml;
        Provider = provider;
        Model = model;
        ReasoningEffort = reasoningEffort;
        Error = null;
        Status = AiBatchJobItemStates.Succeeded;
    }

    public void RecordFailure(string error)
    {
        EnsureNotTerminal();
        Status = AiBatchJobItemStates.Failed;
        Error = error;
    }

    public void MarkApplied(DateTimeOffset now)
    {
        if (Status != AiBatchJobItemStates.Succeeded)
        {
            throw new InvalidOperationException("Only succeeded items can be applied.");
        }

        AppliedAt = now;
        Status = AiBatchJobItemStates.Applied;
        FinishedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureNotTerminal();
        Status = AiBatchJobItemStates.Cancelled;
        FinishedAt = now;
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status == AiBatchJobItemStates.Pending)
        {
            throw new InvalidOperationException("Pending items must start before completion.");
        }

        FinishedAt = now;
    }

    private void EnsureNotTerminal()
    {
        if (Status is AiBatchJobItemStates.Applied or AiBatchJobItemStates.Cancelled)
        {
            throw new InvalidOperationException("Terminal items cannot transition.");
        }
    }
}

public static class AiBatchJobItemStates
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Applied = "applied";
    public const string Cancelled = "cancelled";
}
