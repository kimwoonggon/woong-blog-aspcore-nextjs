using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Pages.Application.GetPageBySlug;

namespace WoongBlog.Api.Modules.Content.Pages.Api.GetPageBySlug;

internal static class GetPageBySlugEndpoint
{
    internal static void MapGetPageBySlug(this IEndpointRouteBuilder app)
    {
        app.MapGet(PagesApiPaths.GetPageBySlug, async (
                string slug,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetPageBySlugQuery(slug), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .RequireRateLimiting("public-read")
            .WithTags("Public Pages")
            .WithName("GetPageBySlug")
            .Produces<PageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
