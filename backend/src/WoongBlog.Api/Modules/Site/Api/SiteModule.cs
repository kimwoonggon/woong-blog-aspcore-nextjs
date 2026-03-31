using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Modules.Site.Api;

internal static class SiteModule
{
    public static WebApplication MapSiteModule(this WebApplication app)
    {
        app.MapSiteEndpoints();
        return app;
    }
}
