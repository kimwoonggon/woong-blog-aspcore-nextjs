using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Modules.AI.Api;

internal static class AiModule
{
    public static WebApplication MapAiModule(this WebApplication app)
    {
        app.MapAiEndpoints();
        return app;
    }
}
