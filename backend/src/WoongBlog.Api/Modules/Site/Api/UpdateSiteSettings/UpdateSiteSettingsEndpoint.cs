using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Common.Api.Validation.Requests;

namespace WoongBlog.Api.Modules.Site.Api.UpdateSiteSettings;

internal static class UpdateSiteSettingsEndpoint
{
    internal static void MapUpdateSiteSettings(this IEndpointRouteBuilder app)
    {
        app.MapPut(SiteApiPaths.UpdateSiteSettings, async (
                UpdateSiteSettingsRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var updated = await sender.Send(request.ToCommand(), cancellationToken);
                return updated.Found ? Results.Ok(new { success = true }) : Results.NotFound();
            })
            .RequireAuthorization("AdminOnly")
            .ValidateRequest<UpdateSiteSettingsRequest>()
            .WithTags("Admin Site")
            .WithName("UpdateSiteSettings")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
