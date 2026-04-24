using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Common.Api.Validation.Requests;
using WoongBlog.Application.Modules.Content.Pages.UpdatePage;

namespace WoongBlog.Api.Modules.Content.Pages.UpdatePage;

internal static class UpdatePageEndpoint
{
    internal static void MapUpdatePage(this IEndpointRouteBuilder app)
    {
        app.MapPut(PagesApiPaths.UpdatePage, async (
                UpdatePageRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var updated = await sender.Send(request.ToCommand(), cancellationToken);
                return updated.Found ? Results.Ok(new { success = true }) : Results.NotFound();
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<UpdatePageRequest>()
            .WithTags("Admin Pages")
            .WithName("UpdatePage")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
