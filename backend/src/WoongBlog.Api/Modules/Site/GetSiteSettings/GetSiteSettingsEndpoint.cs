using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Application.Modules.Site.GetSiteSettings;

namespace WoongBlog.Api.Modules.Site.GetSiteSettings;

internal static class GetSiteSettingsEndpoint
{
    internal static void MapGetSiteSettings(this IEndpointRouteBuilder app)
    {
        app.MapGet(SiteApiPaths.GetSiteSettings, async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetSiteSettingsQuery(), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithTags("Public Site")
            .WithName("GetSiteSettings")
            .Produces<SiteSettingsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
