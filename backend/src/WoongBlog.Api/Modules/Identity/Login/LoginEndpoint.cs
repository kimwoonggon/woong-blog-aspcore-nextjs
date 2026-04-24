using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using WoongBlog.Infrastructure.Auth;

namespace WoongBlog.Api.Modules.Identity.Login;

internal static class LoginEndpoint
{
    internal static void MapLogin(this IEndpointRouteBuilder app)
    {
        app.MapGet(IdentityApiPaths.Login, (
                [AsParameters] LoginRequest request,
                IOptions<AuthOptions> authOptions) =>
            {
                var options = authOptions.Value;
                if (!options.IsConfigured())
                {
                    return Results.Problem(
                        title: "Authentication is not configured",
                        detail: "Set Auth:Enabled, Auth:ClientId, and Auth:ClientSecret in appsettings or environment variables.",
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                var returnUrl = IsLocalReturnUrl(request.ReturnUrl) ? request.ReturnUrl! : "/admin";

                return Results.Challenge(
                    new AuthenticationProperties { RedirectUri = returnUrl },
                    [OpenIdConnectDefaults.AuthenticationScheme]);
            })
            .AllowAnonymous()
            .WithTags("Auth")
            .WithName("AuthLogin");
    }

    private static bool IsLocalReturnUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.StartsWith('/') && !value.StartsWith("//") && !value.StartsWith("/\\");
    }
}
