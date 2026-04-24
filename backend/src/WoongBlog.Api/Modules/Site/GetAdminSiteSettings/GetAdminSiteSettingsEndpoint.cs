using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Site.GetAdminSiteSettings;

namespace WoongBlog.Api.Modules.Site.GetAdminSiteSettings;

internal static class GetAdminSiteSettingsEndpoint
{
    internal static void MapGetAdminSiteSettings(this IEndpointRouteBuilder app)
    {
        app.MapGet(SiteApiPaths.GetAdminSiteSettings, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAdminSiteSettingsQuery(), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin Site")
            .WithName("GetAdminSiteSettings")
            .Produces<AdminSiteSettingsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
