using Microsoft.AspNetCore.Routing;
using MediatR;
using WoongBlog.Api.Modules.AI.Application.BlogFix;

namespace WoongBlog.Api.Modules.AI.Api.BlogFix;

internal static class BlogFixEndpoints
{
    internal static void MapBlogFix(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.BlogFix, async (
                BlogFixRequest request,
                ISender sender,
                CancellationToken cancellationToken) => (await sender.Send(new FixBlogHtmlCommand(
                    request.Html,
                    request.Provider,
                    request.CodexModel,
                    request.CodexReasoningEffort,
                    request.CustomPrompt), cancellationToken)).ToHttpResult())
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiFixBlog");
    }

    internal static void MapBlogFixBatch(this IEndpointRouteBuilder app)
    {
        app.MapPost(AiApiPaths.BlogFixBatch, async (
                BlogFixBatchRequest request,
                ISender sender,
                CancellationToken cancellationToken) => (await sender.Send(new FixBlogBatchCommand(
                    request.BlogIds,
                    request.All,
                    request.Apply,
                    request.Provider,
                    request.CodexModel,
                    request.CodexReasoningEffort,
                    request.CustomPrompt), cancellationToken)).ToHttpResult())
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin AI")
            .WithName("AdminAiFixBlogBatch");
    }
}
