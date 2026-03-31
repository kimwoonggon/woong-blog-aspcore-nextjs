using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorkById;

namespace WoongBlog.Api.Modules.Content.Works.Api.GetAdminWorkById;

internal static class GetAdminWorkByIdEndpoint
{
    internal static void MapGetAdminWorkById(this IEndpointRouteBuilder app)
    {
        app.MapGet(WorksApiPaths.GetAdminWorkById, async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAdminWorkByIdQuery(id), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Works")
            .WithName("GetAdminWorkById")
            .Produces<AdminWorkDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
