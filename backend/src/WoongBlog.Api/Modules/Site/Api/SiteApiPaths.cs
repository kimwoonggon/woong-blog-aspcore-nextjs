using WoongBlog.Api.Common.Api;

namespace WoongBlog.Api.Modules.Site.Api;

internal static class SiteApiPaths
{
    internal const string GetAdminSiteSettings = $"{ApiPaths.Root}/admin/site-settings";
    internal const string UpdateSiteSettings = $"{ApiPaths.Root}/admin/site-settings";
    internal const string GetSiteSettings = $"{ApiPaths.Root}/public/site-settings";
    internal const string GetResume = $"{ApiPaths.Root}/public/resume";
}
