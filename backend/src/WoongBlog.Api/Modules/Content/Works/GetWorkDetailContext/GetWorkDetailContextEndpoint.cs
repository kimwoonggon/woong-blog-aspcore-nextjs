using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Content.Works.GetWorkDetailContext;

namespace WoongBlog.Api.Modules.Content.Works.GetWorkDetailContext;

internal static class GetWorkDetailContextEndpoint
{
    internal static void MapGetWorkDetailContext(this IEndpointRouteBuilder app)
    {
        app.MapGet(WorksApiPaths.GetWorkDetailContext, async (
                string slug,
                int? limit,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetWorkDetailContextQuery(slug, limit ?? 9),
                    cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithTags("Public Works")
            .WithName("GetWorkDetailContext")
            .Produces<WorkDetailContextDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
