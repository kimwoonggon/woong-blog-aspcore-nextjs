namespace WoongBlog.Api.Modules.AI.Application;

public sealed record BlogAiFixResult(
    string FixedHtml,
    string Provider,
    string Model,
    string? ReasoningEffort = null
);
