using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.AI.Application;

namespace WoongBlog.Api.Modules.AI.Api.WorkEnrich;

internal static class WorkEnrichEndpoint
{
    internal static void MapWorkEnrich(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.WorkEnrich, (
                WorkEnrichRequest request,
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.EnrichWorkAsync(request, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiEnrichWork");
    }
}
