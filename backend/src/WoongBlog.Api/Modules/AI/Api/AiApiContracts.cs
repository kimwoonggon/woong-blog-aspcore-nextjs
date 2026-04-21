namespace WoongBlog.Api.Modules.AI.Api;

public sealed record BlogFixRequest(
    string Html,
    string? Provider = null,
    string? CodexModel = null,
    string? CodexReasoningEffort = null,
    string? CustomPrompt = null);

public sealed record BlogFixBatchRequest(
    IReadOnlyList<Guid>? BlogIds,
    bool All,
    bool Apply,
    string? Provider = null,
    string? CodexModel = null,
    string? CodexReasoningEffort = null,
    string? CustomPrompt = null);

public sealed record WorkEnrichRequest(
    string Html,
    string? Title = null,
    string? Provider = null,
    string? CodexModel = null,
    string? CodexReasoningEffort = null,
    string? CustomPrompt = null);

public sealed record BlogFixBatchJobCreateRequest(
    IReadOnlyList<Guid>? BlogIds,
    bool All,
    string? SelectionMode = null,
    string? SelectionLabel = null,
    string? SelectionKey = null,
    bool AutoApply = false,
    int? WorkerCount = null,
    string? Provider = null,
    string? CodexModel = null,
    string? CodexReasoningEffort = null,
    string? CustomPrompt = null);

public sealed record BlogFixBatchJobApplyRequest(
    IReadOnlyList<Guid>? JobItemIds = null);
