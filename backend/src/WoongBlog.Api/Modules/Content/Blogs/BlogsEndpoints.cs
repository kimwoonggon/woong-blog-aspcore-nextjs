using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Content.Blogs.CreateBlog;
using WoongBlog.Api.Modules.Content.Blogs.DeleteBlog;
using WoongBlog.Api.Modules.Content.Blogs.GetAdminBlogById;
using WoongBlog.Api.Modules.Content.Blogs.GetAdminBlogs;
using WoongBlog.Api.Modules.Content.Blogs.GetBlogBySlug;
using WoongBlog.Api.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Api.Modules.Content.Blogs.UpdateBlog;

namespace WoongBlog.Api.Modules.Content.Blogs;

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
