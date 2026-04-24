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
        if (!string.Equals(identityName, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.Role, "admin"),
            new Claim(AuthClaimTypes.ProfileId, Guid.Parse("11111111-1111-1111-1111-111111111111").ToString()),
            new Claim(AuthClaimTypes.SessionId, Guid.Parse("22222222-2222-2222-2222-222222222222").ToString()),
            new Claim(ClaimTypes.Email, "admin@example.com")
        ], Scheme.Name);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
