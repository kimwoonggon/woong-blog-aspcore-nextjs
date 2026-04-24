using WoongBlog.Api.Common.Api;

namespace WoongBlog.Api.Modules.Content.Pages;

internal static class PagesApiPaths
{
    private const string AdminRoot = $"{ApiPaths.Root}/admin/pages";
    private const string PublicRoot = $"{ApiPaths.Root}/public/pages";

    internal const string GetAdminPages = AdminRoot;
    internal const string UpdatePage = AdminRoot;
    internal const string GetPageBySlug = $"{PublicRoot}/{{slug}}";
}
