using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WoongBlog.Infrastructure.Auth;

namespace WoongBlog.Api.Tests;

public class TestOidcChallengeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AuthOptions _authOptions;

    public TestOidcChallengeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<AuthOptions> authOptions)
        : base(options, logger, encoder)
    {
        _authOptions = authOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var callbackUri = $"{Request.Scheme}://{Request.Host}{_authOptions.CallbackPath}";
        var returnUrl = string.IsNullOrWhiteSpace(properties.RedirectUri) ? "/admin" : properties.RedirectUri;
        var location = "https://example.test/oauth/authorize"
                       + $"?client_id={Uri.EscapeDataString(_authOptions.ClientId)}"
                       + $"&redirect_uri={Uri.EscapeDataString(callbackUri)}"
                       + "&response_type=code"
                       + "&scope=openid%20profile%20email"
                       + $"&return_url={Uri.EscapeDataString(returnUrl)}";

        Response.StatusCode = StatusCodes.Status302Found;
        Response.Headers.Location = location;
        return Task.CompletedTask;
    }
}
