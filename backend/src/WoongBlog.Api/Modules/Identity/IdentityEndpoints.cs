using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Identity.GetAdminMembers;
using WoongBlog.Api.Modules.Identity.GetCsrf;
using WoongBlog.Api.Modules.Identity.GetSession;
using WoongBlog.Api.Modules.Identity.Login;
using WoongBlog.Api.Modules.Identity.Logout;
using WoongBlog.Api.Modules.Identity.TestLogin;

namespace WoongBlog.Api.Modules.Identity;

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
