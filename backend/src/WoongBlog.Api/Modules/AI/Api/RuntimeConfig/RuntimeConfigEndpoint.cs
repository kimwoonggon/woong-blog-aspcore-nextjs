using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.AI.Application;

namespace WoongBlog.Api.Modules.AI.Api.RuntimeConfig;

internal static class RuntimeConfigEndpoint
{
    internal static void MapAiRuntimeConfig(this IEndpointRouteBuilder app)
    {
        app.MapGet(AiApiPaths.RuntimeConfig, (IAiAdminService service) => service.RuntimeConfig())
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiRuntimeConfig");
    }
}
