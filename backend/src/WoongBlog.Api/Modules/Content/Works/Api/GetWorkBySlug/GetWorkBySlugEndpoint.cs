using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorkBySlug;

namespace WoongBlog.Api.Modules.Content.Works.Api.GetWorkBySlug;

internal static class GetWorkBySlugEndpoint
{
    internal static void MapGetWorkBySlug(this IEndpointRouteBuilder app)
    {
        app.MapGet(WorksApiPaths.GetWorkBySlug, async (
                string slug,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetWorkBySlugQuery(slug), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithTags("Public Works")
            .WithName("GetWorkBySlug")
            .Produces<WorkDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
