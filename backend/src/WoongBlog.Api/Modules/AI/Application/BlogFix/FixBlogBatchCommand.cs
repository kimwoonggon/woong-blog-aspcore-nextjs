using MediatR;
using WoongBlog.Api.Modules.AI.Api;

namespace WoongBlog.Api.Modules.AI.Application.BlogFix;

public sealed record FixBlogBatchCommand(
    IReadOnlyList<Guid>? BlogIds,
    bool All,
    bool Apply,
    string? Provider,
    string? CodexModel,
    string? CodexReasoningEffort,
    string? CustomPrompt) : IRequest<AiActionResult<BlogFixBatchResponse>>;
