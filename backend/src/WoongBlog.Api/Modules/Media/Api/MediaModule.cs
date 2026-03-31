using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Modules.Media.Api;

internal static class MediaModule
{
    public static WebApplication MapMediaModule(this WebApplication app)
    {
        app.MapMediaEndpoints();
        return app;
    }
}
