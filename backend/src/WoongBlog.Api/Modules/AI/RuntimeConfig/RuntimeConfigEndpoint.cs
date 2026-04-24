using Microsoft.AspNetCore.Routing;
using MediatR;
using WoongBlog.Application.Modules.AI.RuntimeConfig;

namespace WoongBlog.Api.Modules.AI.RuntimeConfig;

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
