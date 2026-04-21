using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Modules.Identity.Application;

public sealed class IdentityInteractionService : IIdentityInteractionService
{
    private readonly WoongBlogDbContext _dbContext;
    private readonly AuthRecorder _authRecorder;
    private readonly AuthOptions _authOptions;

    public IdentityInteractionService(
        WoongBlogDbContext dbContext,
        AuthRecorder authRecorder,
        IOptions<AuthOptions> authOptions)
    {
        _dbContext = dbContext;
        _authRecorder = authRecorder;
        _authOptions = authOptions.Value;
    }

    public async Task<string> LogoutAsync(
        ClaimsPrincipal principal,
        HttpContext httpContext,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        await _authRecorder.RecordLogoutAsync(principal, httpContext, cancellationToken);
        return string.IsNullOrWhiteSpace(returnUrl) ? _authOptions.SignedOutRedirectPath : returnUrl;
    }

    public async Task<IdentityTestLoginResult?> CreateTestLoginAsync(
        HttpContext httpContext,
        string email,
        string returnUrl,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var oidcPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, profile.ProviderSubject),
            new Claim(ClaimTypes.Email, profile.Email),
            new Claim("name", string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.Email : profile.DisplayName)
        ], "test-login"));

        var result = await _authRecorder.RecordSuccessfulLoginAsync(oidcPrincipal, httpContext, cancellationToken);

        var appPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, profile.ProviderSubject),
            new Claim(ClaimTypes.Email, result.Email),
            new Claim(ClaimTypes.Name, result.DisplayName),
            new Claim(ClaimTypes.Role, result.Role),
            new Claim(AuthClaimTypes.ProfileId, result.ProfileId.ToString()),
            new Claim(AuthClaimTypes.Role, result.Role),
            new Claim(AuthClaimTypes.SessionId, result.SessionId.ToString())
        ], CookieAuthenticationDefaults.AuthenticationScheme));

        var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/admin" : returnUrl;

        return new IdentityTestLoginResult(
            appPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(_authOptions.SlidingExpirationMinutes),
                AllowRefresh = true,
                RedirectUri = redirectUri
            },
            redirectUri);
    }
}
