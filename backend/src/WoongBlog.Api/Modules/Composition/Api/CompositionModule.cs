using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Modules.Composition.Api;

internal static class CompositionModule
{
    public static WebApplication MapCompositionModule(this WebApplication app)
    {
        app.MapCompositionEndpoints();
        return app;
    }
}
