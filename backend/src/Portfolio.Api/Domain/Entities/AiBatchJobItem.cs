namespace Portfolio.Api.Domain.Entities;

public class AiBatchJobItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Guid EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = AiBatchJobItemStates.Pending;
    public string? FixedHtml { get; set; }
    public string? Error { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public string? ReasoningEffort { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
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
