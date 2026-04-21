using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using WoongBlog.Api.Modules.Identity.Application;

namespace WoongBlog.Api.Modules.Identity.Api.TestLogin;

internal static class TestLoginEndpoint
{
    internal static void MapTestLogin(this IEndpointRouteBuilder app)
    {
        app.MapGet(IdentityApiPaths.TestLogin, async (
                HttpContext httpContext,
                [AsParameters] TestLoginRequest request,
                IWebHostEnvironment environment,
                IConfiguration configuration,
                IIdentityInteractionService identityInteractionService) =>
            {
                if (!configuration.GetValue<bool>("Auth:EnableTestLoginEndpoint"))
                {
                    return Results.NotFound();
                }

                if (!(environment.IsDevelopment() || environment.IsEnvironment("Testing")))
                {
                    return Results.NotFound();
                }

                var testLogin = await identityInteractionService.CreateTestLoginAsync(
                    httpContext,
                    request.Email,
                    request.ReturnUrl,
                    httpContext.RequestAborted);

                if (testLogin is null)
                {
                    return Results.NotFound(new { message = "Seeded profile not found." });
                }

                var role = testLogin.Principal.FindFirst(AuthClaimTypes.Role)?.Value
                    ?? testLogin.Principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.LocalRedirect("/login?error=admin_only");
                }

                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    testLogin.Principal,
                    testLogin.Properties);

                return Results.LocalRedirect(testLogin.RedirectUri);
            })
            .AllowAnonymous()
            .WithTags("Auth")
            .WithName("AuthTestLogin");
    }
}
