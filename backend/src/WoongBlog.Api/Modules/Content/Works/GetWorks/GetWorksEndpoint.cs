using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Content.Works.GetWorks;

namespace WoongBlog.Api.Modules.Content.Works.GetWorks;

internal static class GetWorksEndpoint
{
    internal static void MapGetWorks(this IEndpointRouteBuilder app)
    {
        app.MapGet(WorksApiPaths.GetWorks, async (
                [AsParameters] GetWorksRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToQuery(), cancellationToken);
                return Results.Ok(result);
            })
            .WithTags("Public Works")
            .WithName("GetWorks")
            .Produces<PagedWorksDto>(StatusCodes.Status200OK);
    }
}
