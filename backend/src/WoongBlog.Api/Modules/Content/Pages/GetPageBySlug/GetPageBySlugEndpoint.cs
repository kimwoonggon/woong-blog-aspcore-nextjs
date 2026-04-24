using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;

namespace WoongBlog.Api.Modules.Content.Pages.GetPageBySlug;

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
            .WithTags("Public Pages")
            .WithName("GetPageBySlug")
            .Produces<PageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
