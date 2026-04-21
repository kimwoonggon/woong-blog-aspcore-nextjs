namespace WoongBlog.Api.Modules.AI.Application;

public sealed record AiFixRequestOptions(
    AiFixMode Mode = AiFixMode.BlogFix,
    string? Title = null,
    string? Provider = null,
    string? CodexModel = null,
    string? CodexReasoningEffort = null,
    string? CustomPrompt = null
);

public enum AiFixMode
{
    BlogFix = 0,
    WorkEnrich = 1
}
