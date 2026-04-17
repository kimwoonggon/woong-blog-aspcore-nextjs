using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Composition.Application.GetHome;

namespace WoongBlog.Api.Modules.Composition.Api.GetHome;

internal static class GetHomeEndpoint
{
    internal static void MapGetHome(this IEndpointRouteBuilder app)
    {
        app.MapGet(CompositionApiPaths.GetHome, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetHomeQuery(), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithTags("Public Home")
            .WithName("GetHome")
            .Produces<HomeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
