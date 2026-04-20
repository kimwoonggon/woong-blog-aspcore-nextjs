using MediatR;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Modules.AI.Api;

namespace WoongBlog.Api.Modules.AI.Application.WorkEnrich;

public sealed class EnrichWorkHtmlCommandHandler(IBlogAiFixService aiFixService)
    : IRequestHandler<EnrichWorkHtmlCommand, AiActionResult<WorkEnrichResponse>>
{
    public async Task<AiActionResult<WorkEnrichResponse>> Handle(EnrichWorkHtmlCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Html))
        {
            return AiActionResult<WorkEnrichResponse>.BadRequest("HTML content is required.");
        }

        var result = await aiFixService.FixHtmlAsync(request.Html, cancellationToken, new AiFixRequestOptions(
            Mode: AiFixMode.WorkEnrich,
            Title: request.Title,
            Provider: request.Provider,
            CodexModel: request.CodexModel,
            CodexReasoningEffort: request.CodexReasoningEffort,
            CustomPrompt: request.CustomPrompt));

        return AiActionResult<WorkEnrichResponse>.Ok(
            new WorkEnrichResponse(result.FixedHtml, result.Provider, result.Model, result.ReasoningEffort));
    }
}
