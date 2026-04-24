using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Modules.Content.Pages;

internal static class PagesModule
{
    public static WebApplication MapPagesModule(this WebApplication app)
    {
        app.MapPagesEndpoints();
        return app;
    }
}
