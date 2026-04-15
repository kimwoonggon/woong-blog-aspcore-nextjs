namespace WoongBlog.Api.Infrastructure.Ai;

public sealed record AiFixRequestOptions(
    AiFixMode Mode = AiFixMode.BlogFix,
    string? Title = null,
    string? Provider = null,
    string? CodexModel = null,
    string? CodexReasoningEffort = null
);

public enum AiFixMode
{
    BlogFix = 0,
    WorkEnrich = 1
}
