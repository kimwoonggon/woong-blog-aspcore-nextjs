using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WoongBlog.Infrastructure.Auth;
using WoongBlog.Application.Modules.Identity;

namespace WoongBlog.Api.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string HeaderName = "X-Test-Auth";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identityName = Request.Headers[HeaderName].ToString();
        var identity = identityName.ToLowerInvariant() switch
        {
            "admin" => CreateIdentity(
                "admin",
                "admin@example.com",
                "Admin Example",
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222")),
            "user" => CreateIdentity(
                "user",
                "user@example.com",
                "User Example",
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("44444444-4444-4444-4444-444444444444")),
            _ => null
        };

        if (identity is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private ClaimsIdentity CreateIdentity(
        string role,
        string email,
        string displayName,
        Guid profileId,
        Guid sessionId)
    {
        return new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.Role, role),
            new Claim(ClaimTypes.Role, role),
            new Claim(AuthClaimTypes.ProfileId, profileId.ToString()),
            new Claim(AuthClaimTypes.SessionId, sessionId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, displayName)
        ], Scheme.Name);
    }
}
