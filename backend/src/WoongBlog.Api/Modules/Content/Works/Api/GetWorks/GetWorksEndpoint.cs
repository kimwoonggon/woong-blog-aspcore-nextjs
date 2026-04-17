using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorks;

namespace WoongBlog.Api.Modules.Content.Works.Api.GetWorks;

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
            .RequireRateLimiting("public-read")
            .WithTags("Public Works")
            .WithName("GetWorks")
            .Produces<PagedWorksDto>(StatusCodes.Status200OK);
    }
}
