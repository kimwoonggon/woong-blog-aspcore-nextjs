using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace WoongBlog.Api.Modules.Identity.Infrastructure;

public interface IIdentityInteractionService
{
    Task<string> LogoutAsync(
        ClaimsPrincipal principal,
        HttpContext httpContext,
        string? returnUrl,
        CancellationToken cancellationToken);

    Task<IdentityTestLoginResult?> CreateTestLoginAsync(
        HttpContext httpContext,
        string email,
        string returnUrl,
        CancellationToken cancellationToken);
}

public sealed record IdentityTestLoginResult(
    ClaimsPrincipal Principal,
    AuthenticationProperties Properties,
    string RedirectUri);
