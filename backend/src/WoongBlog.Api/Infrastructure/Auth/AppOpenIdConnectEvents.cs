using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Infrastructure.Auth;

internal sealed class AppOpenIdConnectEvents(
    AuthRecorder recorder,
    IOptions<AuthOptions> authOptions) : OpenIdConnectEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var result = await recorder.RecordSuccessfulLoginAsync(
            context.Principal!,
            context.HttpContext,
            context.HttpContext.RequestAborted);

        if (!string.Equals(result.Role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            await recorder.RecordDeniedAccessAsync(
                context.Principal!,
                context.HttpContext,
                "Admin role required for login.",
                context.HttpContext.RequestAborted);

            await recorder.RevokeSessionAsync(
                result.SessionId,
                "non_admin_login_blocked",
                context.HttpContext.RequestAborted);

            context.Response.Redirect("/login?error=admin_only");
            context.HandleResponse();
            return;
        }

        context.Properties ??= new AuthenticationProperties();

        if (context.Principal?.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim(AuthClaimTypes.ProfileId, result.ProfileId.ToString()));
            identity.AddClaim(new Claim(AuthClaimTypes.Role, result.Role));
            identity.AddClaim(new Claim(AuthClaimTypes.SessionId, result.SessionId.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Role, result.Role));

            if (!identity.HasClaim(x => x.Type == ClaimTypes.Email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, result.Email));
            }
        }

        context.Properties.IssuedUtc = DateTimeOffset.UtcNow;
        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(authOptions.Value.SlidingExpirationMinutes);
        context.Properties.AllowRefresh = true;
    }

    public override async Task RemoteFailure(RemoteFailureContext context)
    {
        await recorder.RecordLoginFailureAsync(
            context.HttpContext,
            context.Failure?.Message ?? "OIDC remote failure",
            context.HttpContext.RequestAborted);

        context.Response.Redirect("/login?error=auth_failed");
        context.HandleResponse();
    }
}
