namespace WoongBlog.Api.Domain.Entities;

public class AiBatchJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TargetType { get; set; } = "blog";
    public string Status { get; set; } = AiBatchJobStates.Queued;
    public string SelectionMode { get; set; } = "selected";
    public string SelectionLabel { get; set; } = string.Empty;
    public string SelectionKey { get; set; } = string.Empty;
    public bool All { get; set; }
    public bool AutoApply { get; set; }
    public int? WorkerCount { get; set; }
    public bool CancelRequested { get; set; }
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? ReasoningEffort { get; set; }
    public string PromptMode { get; set; } = "blog-fix";
    public Guid? RequestedByProfileId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class AiBatchJobStates
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}
