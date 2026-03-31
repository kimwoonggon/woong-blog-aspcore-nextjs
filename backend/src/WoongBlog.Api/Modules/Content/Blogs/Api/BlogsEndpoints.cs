using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Blogs.Api.CreateBlog;
using WoongBlog.Api.Modules.Content.Blogs.Api.DeleteBlog;
using WoongBlog.Api.Modules.Content.Blogs.Api.GetAdminBlogById;
using WoongBlog.Api.Modules.Content.Blogs.Api.GetAdminBlogs;
using WoongBlog.Api.Modules.Content.Blogs.Api.GetBlogBySlug;
using WoongBlog.Api.Modules.Content.Blogs.Api.GetBlogs;
using WoongBlog.Api.Modules.Content.Blogs.Api.UpdateBlog;

namespace WoongBlog.Api.Modules.Content.Blogs.Api;

internal static class BlogsEndpoints
{
    internal static void MapBlogsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetAdminBlogs();
        app.MapGetAdminBlogById();
        app.MapCreateBlog();
        app.MapUpdateBlog();
        app.MapDeleteBlog();
        app.MapGetBlogs();
        app.MapGetBlogBySlug();
    }
}
