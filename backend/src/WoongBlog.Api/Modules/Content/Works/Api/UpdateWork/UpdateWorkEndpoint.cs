using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Common.Api.Validation.Requests;

namespace WoongBlog.Api.Modules.Content.Works.Api.UpdateWork;

internal static class UpdateWorkEndpoint
{
    internal static void MapUpdateWork(this IEndpointRouteBuilder app)
    {
        app.MapPut(WorksApiPaths.UpdateWork, async (
                Guid id,
                UpdateWorkRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(new { result.Id, result.Slug });
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<UpdateWorkRequest>()
            .WithTags("Admin Works")
            .WithName("UpdateWork")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
