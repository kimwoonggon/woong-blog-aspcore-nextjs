using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Modules.Site;

internal static class SiteModule
{
    public static WebApplication MapSiteModule(this WebApplication app)
    {
        app.MapSiteEndpoints();
        return app;
    }
}
