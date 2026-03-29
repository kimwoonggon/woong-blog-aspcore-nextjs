namespace WoongBlog.Api.Infrastructure.Ai;

public sealed record BlogAiFixResult(
    string FixedHtml,
    string Provider,
    string Model,
    string? ReasoningEffort = null
);
