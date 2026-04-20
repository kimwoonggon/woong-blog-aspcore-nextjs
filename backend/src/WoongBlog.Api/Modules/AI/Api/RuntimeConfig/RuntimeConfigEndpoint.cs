using Microsoft.AspNetCore.Routing;
using MediatR;
using WoongBlog.Api.Modules.AI.Application.RuntimeConfig;

namespace WoongBlog.Api.Modules.AI.Api.RuntimeConfig;

internal static class RuntimeConfigEndpoint
{
    internal static void MapAiRuntimeConfig(this IEndpointRouteBuilder app)
    {
        app.MapGet(AiApiPaths.RuntimeConfig, async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetAiRuntimeConfigQuery(), cancellationToken)))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiRuntimeConfig");
    }
}
