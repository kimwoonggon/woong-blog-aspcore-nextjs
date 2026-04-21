using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WoongBlog.Api.Modules.Identity.Infrastructure;

namespace WoongBlog.Api.Modules.Identity.Api.Logout;

internal static class LogoutEndpoint
{
    internal static void MapLogout(this IEndpointRouteBuilder app)
    {
        app.MapPost(IdentityApiPaths.Logout, async (
                HttpContext httpContext,
                [AsParameters] LogoutRequest request,
                IIdentityInteractionService identityInteractionService,
                CancellationToken cancellationToken) =>
            {
                var target = await identityInteractionService.LogoutAsync(
                    httpContext.User,
                    httpContext,
                    request.ReturnUrl,
                    cancellationToken);

                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Ok(new { redirectUrl = target });
            })
            .WithTags("Auth")
            .WithName("AuthLogout");
    }

    internal static void MapLogoutGet(this IEndpointRouteBuilder app)
    {
        app.MapGet(IdentityApiPaths.Logout, () =>
            Results.Problem(
                title: "Logout requires POST",
                detail: "Use POST /api/auth/logout with an anti-forgery token.",
                statusCode: StatusCodes.Status405MethodNotAllowed))
            .AllowAnonymous()
            .WithTags("Auth")
            .WithName("AuthLogoutGet");
    }
}
