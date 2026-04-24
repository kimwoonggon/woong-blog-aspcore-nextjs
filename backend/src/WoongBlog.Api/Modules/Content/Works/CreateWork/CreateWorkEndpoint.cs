using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Common.Api.Validation.Requests;

namespace WoongBlog.Api.Modules.Content.Works.CreateWork;

internal static class CreateWorkEndpoint
{
    internal static void MapCreateWork(this IEndpointRouteBuilder app)
    {
        app.MapPost(WorksApiPaths.CreateWork, async (
                CreateWorkRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(), cancellationToken);
                return Results.Ok(new { result.Id, result.Slug });
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<CreateWorkRequest>()
            .WithTags("Admin Works")
            .WithName("CreateWork")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
