namespace WoongBlog.Application.Modules.AI;

public sealed record BlogAiFixResult(
    string FixedHtml,
    string Provider,
    string Model,
    string? ReasoningEffort = null
);
