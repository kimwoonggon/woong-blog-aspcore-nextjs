using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Modules.Content.Works;

internal static class WorksModule
{
    public static WebApplication MapWorksModule(this WebApplication app)
    {
        app.MapWorksEndpoints();
        return app;
    }
}
