using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Identity.Api.GetAdminMembers;
using WoongBlog.Api.Modules.Identity.Api.GetCsrf;
using WoongBlog.Api.Modules.Identity.Api.GetSession;
using WoongBlog.Api.Modules.Identity.Api.Login;
using WoongBlog.Api.Modules.Identity.Api.Logout;
using WoongBlog.Api.Modules.Identity.Api.TestLogin;

namespace WoongBlog.Api.Modules.Identity.Api;

internal static class IdentityEndpoints
{
    internal static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapLogin();
        app.MapGetSession();
        app.MapGetCsrf();
        app.MapLogout();
        app.MapLogoutGet();
        app.MapTestLogin();
        app.MapGetAdminMembers();
    }
}
