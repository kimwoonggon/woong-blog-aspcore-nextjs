using Microsoft.AspNetCore.Routing;
using MediatR;
using WoongBlog.Api.Modules.AI.Application.WorkEnrich;

namespace WoongBlog.Api.Modules.AI.Api.WorkEnrich;

internal static class WorkEnrichEndpoint
{
    internal static void MapWorkEnrich(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.WorkEnrich, async (
                WorkEnrichRequest request,
                ISender sender,
                CancellationToken cancellationToken) => (await sender.Send(new EnrichWorkHtmlCommand(
                    request.Html,
                    request.Title,
                    request.Provider,
                    request.CodexModel,
                    request.CodexReasoningEffort,
                    request.CustomPrompt), cancellationToken)).ToHttpResult())
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiEnrichWork");
    }
}
