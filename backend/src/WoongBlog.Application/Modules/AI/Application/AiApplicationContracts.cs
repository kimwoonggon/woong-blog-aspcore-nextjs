namespace WoongBlog.Api.Modules.AI.Application;

public sealed record BlogFixResponse(string FixedHtml, string Provider, string Model, string? ReasoningEffort);

public sealed record BlogFixBatchItemResponse(
    Guid BlogId,
    string Title,
    string Status,
    string? FixedHtml,
    string? Error,
    string? Provider,
    string? Model,
    string? ReasoningEffort);

public sealed record BlogFixBatchResponse(
    IReadOnlyList<BlogFixBatchItemResponse> Results,
    bool Applied);

public sealed record WorkEnrichResponse(
    string FixedHtml,
    string Provider,
    string Model,
    string? ReasoningEffort);

public sealed record AiRuntimeConfigResponse(
    string Provider,
    IReadOnlyList<string> AvailableProviders,
    string DefaultModel,
    string CodexModel,
    string CodexReasoningEffort,
    IReadOnlyList<string> AllowedCodexModels,
    IReadOnlyList<string> AllowedCodexReasoningEfforts,
    int BatchConcurrency,
    int BatchCompletedRetentionDays,
    string DefaultSystemPrompt,
    string DefaultBlogFixPrompt,
    string DefaultWorkEnrichPrompt);

public sealed record BlogFixBatchJobCancelQueuedResponse(int Cancelled);

public sealed record BlogFixBatchJobClearCompletedResponse(int Cleared);

public sealed record BlogFixBatchJobRemoveResponse(int Removed, Guid JobId);

public sealed record BlogFixBatchJobListResponse(
    IReadOnlyList<BlogFixBatchJobSummaryResponse> Jobs,
    int RunningCount,
    int QueuedCount,
    int CompletedCount,
    int FailedCount,
    int CancelledCount);

public sealed record BlogFixBatchJobSummaryResponse(
    Guid JobId,
    string Status,
    string SelectionMode,
    string SelectionLabel,
    string SelectionKey,
    bool AutoApply,
    int? WorkerCount,
    int TotalCount,
    int ProcessedCount,
    int SucceededCount,
    int FailedCount,
    string Provider,
    string Model,
    string? ReasoningEffort,
    string? CustomPrompt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    bool CancelRequested);

public sealed record BlogFixBatchJobItemResponse(
    Guid JobItemId,
    Guid BlogId,
    string Title,
    string Status,
    string? FixedHtml,
    string? Error,
    string? Provider,
    string? Model,
    string? ReasoningEffort,
    DateTimeOffset? AppliedAt);

public sealed record BlogFixBatchJobDetailResponse(
    Guid JobId,
    string Status,
    string SelectionMode,
    string SelectionLabel,
    string SelectionKey,
    bool AutoApply,
    int? WorkerCount,
    int TotalCount,
    int ProcessedCount,
    int SucceededCount,
    int FailedCount,
    string Provider,
    string Model,
    string? ReasoningEffort,
    string? CustomPrompt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    bool CancelRequested,
    IReadOnlyList<BlogFixBatchJobItemResponse> Items);
