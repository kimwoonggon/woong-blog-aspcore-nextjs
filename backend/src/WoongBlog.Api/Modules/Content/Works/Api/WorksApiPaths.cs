using WoongBlog.Api.Common.Api;

namespace WoongBlog.Api.Modules.Content.Works.Api;

internal static class WorksApiPaths
{
    private const string AdminRoot = $"{ApiPaths.Root}/admin/works";
    private const string PublicRoot = $"{ApiPaths.Root}/public/works";

    internal const string GetAdminWorks = AdminRoot;
    internal const string GetAdminWorkById = $"{AdminRoot}/{{id:guid}}";
    internal const string CreateWork = AdminRoot;
    internal const string UpdateWork = $"{AdminRoot}/{{id:guid}}";
    internal const string DeleteWork = $"{AdminRoot}/{{id:guid}}";
    internal const string GetWorks = PublicRoot;
    internal const string GetWorkBySlug = $"{PublicRoot}/{{slug}}";
}
