using MediatR;
using WoongBlog.Application.Modules.AI;

namespace WoongBlog.Application.Modules.AI.BlogFix;

public sealed record FixBlogHtmlCommand(
    string Html,
    string? Provider,
    string? CodexModel,
    string? CodexReasoningEffort,
    string? CustomPrompt) : IRequest<AiActionResult<BlogFixResponse>>;
