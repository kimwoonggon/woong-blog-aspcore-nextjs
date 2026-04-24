using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Site.GetAdminSiteSettings;
using WoongBlog.Api.Modules.Site.GetResume;
using WoongBlog.Api.Modules.Site.GetSiteSettings;
using WoongBlog.Api.Modules.Site.UpdateSiteSettings;

namespace WoongBlog.Api.Modules.Site;

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
