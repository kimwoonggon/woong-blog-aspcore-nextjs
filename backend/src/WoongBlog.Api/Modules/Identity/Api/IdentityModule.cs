using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Modules.Identity.Api;

internal static class IdentityModule
{
    public static WebApplication MapIdentityModule(this WebApplication app)
    {
        app.MapIdentityEndpoints();
        return app;
    }
}
