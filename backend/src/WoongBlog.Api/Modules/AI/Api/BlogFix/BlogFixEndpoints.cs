using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.AI.Application;

namespace WoongBlog.Api.Modules.AI.Api.BlogFix;

internal static class BlogFixEndpoints
{
    internal static void MapBlogFix(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.BlogFix, (
                BlogFixRequest request,
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.FixBlogAsync(request, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiFixBlog");
    }

    internal static void MapBlogFixBatch(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.BlogFixBatch, (
                BlogFixBatchRequest request,
                IAiAdminService service,
                CancellationToken cancellationToken) =>
            service.FixBlogBatchAsync(request, cancellationToken))
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiFixBlogBatch");
    }
}
