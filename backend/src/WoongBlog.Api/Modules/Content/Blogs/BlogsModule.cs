using Microsoft.AspNetCore.Builder;

namespace WoongBlog.Api.Modules.Content.Blogs;

internal static class BlogsModule
{
    public static WebApplication MapBlogsModule(this WebApplication app)
    {
        app.MapBlogsEndpoints();
        return app;
    }
}
