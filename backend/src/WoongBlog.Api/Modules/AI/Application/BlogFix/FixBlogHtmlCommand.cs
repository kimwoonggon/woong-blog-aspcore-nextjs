using MediatR;
using WoongBlog.Api.Modules.AI.Api;

namespace WoongBlog.Api.Modules.AI.Application.BlogFix;

public sealed record FixBlogHtmlCommand(
    string Html,
    string? Provider,
    string? CodexModel,
    string? CodexReasoningEffort,
    string? CustomPrompt) : IRequest<AiActionResult<BlogFixResponse>>;
