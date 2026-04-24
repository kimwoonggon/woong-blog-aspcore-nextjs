using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Content.Works.DeleteWork;

namespace WoongBlog.Api.Modules.Content.Works.DeleteWork;

internal static class DeleteWorkEndpoint
{
    internal static void MapDeleteWork(this IEndpointRouteBuilder app)
    {
        app.MapDelete(WorksApiPaths.DeleteWork, async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var deleted = await sender.Send(new DeleteWorkCommand(id), cancellationToken);
                return deleted.Found ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Works")
            .WithName("DeleteWork")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
}
