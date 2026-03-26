using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Infrastructure.Auth;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Api.Infrastructure.Security;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthOptions _authOptions;
    private readonly SecurityOptions _securityOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly PortfolioDbContext _dbContext;
    private readonly IAntiforgery _antiforgery;

    public AuthController(
        Microsoft.Extensions.Options.IOptions<AuthOptions> authOptions,
        Microsoft.Extensions.Options.IOptions<SecurityOptions> securityOptions,
        IWebHostEnvironment environment,
        PortfolioDbContext dbContext,
        IAntiforgery antiforgery)
    {
        _authOptions = authOptions.Value;
        _securityOptions = securityOptions.Value;
        _environment = environment;
        _dbContext = dbContext;
        _antiforgery = antiforgery;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public IActionResult Login([FromQuery] string? returnUrl = "/admin")
    {
        if (!_authOptions.IsConfigured())
        {
            return Problem(
                title: "Authentication is not configured",
                detail: "Set Auth:Enabled, Auth:ClientId, and Auth:ClientSecret in appsettings or environment variables.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/admin";
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("session")]
    [AllowAnonymous]
    public IActionResult Session()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return Ok(new { authenticated = false });
        }

        return Ok(new
        {
            authenticated = true,
            name = User.Identity?.Name,
            email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
            role = User.FindFirstValue(AuthClaimTypes.Role),
            profileId = User.FindFirstValue(AuthClaimTypes.ProfileId)
        });
    }

    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new
        {
            requestToken = tokens.RequestToken,
            headerName = _securityOptions.AntiforgeryHeaderName
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] string? returnUrl = null)
    {
        var recorder = HttpContext.RequestServices.GetRequiredService<AuthRecorder>();
        await recorder.RecordLogoutAsync(User, HttpContext);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var target = string.IsNullOrWhiteSpace(returnUrl) ? _authOptions.SignedOutRedirectPath : returnUrl;
        return Ok(new { redirectUrl = target });
    }

    [HttpGet("logout")]
    [AllowAnonymous]
    public IActionResult LogoutGet() =>
        Problem(
            title: "Logout requires POST",
            detail: "Use POST /api/auth/logout with an anti-forgery token.",
            statusCode: StatusCodes.Status405MethodNotAllowed);

    [HttpGet("test-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> TestLogin([FromQuery] string email = "admin@example.com", [FromQuery] string returnUrl = "/admin")
    {
        if (!(_environment.IsDevelopment() || _environment.IsEnvironment("Testing")))
        {
            return NotFound();
        }

        var profile = await _dbContext.Profiles.AsNoTracking().SingleOrDefaultAsync(x => x.Email == email);
        if (profile is null)
        {
            return NotFound(new { message = "Seeded profile not found." });
        }

        var oidcPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, profile.ProviderSubject),
            new Claim(ClaimTypes.Email, profile.Email),
            new Claim("name", string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.Email : profile.DisplayName)
        ], "test-login"));

        var recorder = HttpContext.RequestServices.GetRequiredService<AuthRecorder>();
        var result = await recorder.RecordSuccessfulLoginAsync(oidcPrincipal, HttpContext, HttpContext.RequestAborted);

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

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            appPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(_authOptions.SlidingExpirationMinutes),
                AllowRefresh = true,
                RedirectUri = returnUrl
            });

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/admin" : returnUrl);
    }
}
