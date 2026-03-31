using WoongBlog.Api.Common.Api;

namespace WoongBlog.Api.Modules.Content.Blogs.Api;

internal static class BlogsApiPaths
{
    private const string AdminRoot = $"{ApiPaths.Root}/admin/blogs";
    private const string PublicRoot = $"{ApiPaths.Root}/public/blogs";

    internal const string GetAdminBlogs = AdminRoot;
    internal const string GetAdminBlogById = $"{AdminRoot}/{{id:guid}}";
    internal const string CreateBlog = AdminRoot;
    internal const string UpdateBlog = $"{AdminRoot}/{{id:guid}}";
    internal const string DeleteBlog = $"{AdminRoot}/{{id:guid}}";
    internal const string GetBlogs = PublicRoot;
    internal const string GetBlogBySlug = $"{PublicRoot}/{{slug}}";
}
