using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Site.Api.GetAdminSiteSettings;
using WoongBlog.Api.Modules.Site.Api.GetResume;
using WoongBlog.Api.Modules.Site.Api.GetSiteSettings;
using WoongBlog.Api.Modules.Site.Api.UpdateSiteSettings;

namespace WoongBlog.Api.Modules.Site.Api;

internal static class SiteEndpoints
{
    internal static void MapSiteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetAdminSiteSettings();
        app.MapUpdateSiteSettings();
        app.MapGetSiteSettings();
        app.MapGetResume();
    }
}
