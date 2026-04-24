using MediatR;
using WoongBlog.Application.Modules.AI;

namespace WoongBlog.Application.Modules.AI.BlogFix;

public sealed record FixBlogBatchCommand(
    IReadOnlyList<Guid>? BlogIds,
    bool All,
    bool Apply,
    string? Provider,
    string? CodexModel,
    string? CodexReasoningEffort,
    string? CustomPrompt) : IRequest<AiActionResult<BlogFixBatchResponse>>;
