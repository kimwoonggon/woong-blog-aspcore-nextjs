using WoongBlog.Api.Common.Api;

namespace WoongBlog.Api.Modules.Identity.Api;

internal static class IdentityApiPaths
{
    internal const string Login = $"{ApiPaths.Root}/auth/login";
    internal const string Session = $"{ApiPaths.Root}/auth/session";
    internal const string Csrf = $"{ApiPaths.Root}/auth/csrf";
    internal const string Logout = $"{ApiPaths.Root}/auth/logout";
    internal const string TestLogin = $"{ApiPaths.Root}/auth/test-login";
    internal const string Members = $"{ApiPaths.Root}/admin/members";
}
