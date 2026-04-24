using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WoongBlog.Infrastructure.Auth;

internal sealed class AppCookieAuthenticationEvents(AuthRecorder recorder) : CookieAuthenticationEvents
{
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect("/login?reason=session_expired");
        return Task.CompletedTask;
    }

    public override async Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        await recorder.RecordDeniedAccessAsync(
            context.HttpContext.User,
            context.HttpContext,
            "access_denied",
            context.HttpContext.RequestAborted);

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        context.Response.Redirect("/login?reason=access_denied");
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var isValid = await recorder.ValidateSessionAsync(context.Principal!, context.HttpContext.RequestAborted);
        if (!isValid)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
