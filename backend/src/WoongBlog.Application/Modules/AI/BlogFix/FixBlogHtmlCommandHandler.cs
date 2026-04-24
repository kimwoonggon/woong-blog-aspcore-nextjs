using MediatR;
using WoongBlog.Application.Modules.AI;

namespace WoongBlog.Application.Modules.AI.BlogFix;

public sealed class FixBlogHtmlCommandHandler(IBlogAiFixService aiFixService)
    : IRequestHandler<FixBlogHtmlCommand, AiActionResult<BlogFixResponse>>
{
    public async Task<AiActionResult<BlogFixResponse>> Handle(FixBlogHtmlCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return AiActionResult<BlogFixResponse>.BadRequest("HTML content is required.");
        }

        var result = await aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.BlogFix,
            Provider: request.Provider,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort,
            CustomPrompt: request.CustomPrompt));

        return AiActionResult<BlogFixResponse>.Ok(
            new BlogFixResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }
}
