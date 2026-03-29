using System.Security.Claims;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Infrastructure.Auth;

public sealed class AuthAuditService : IAuthAuditService
{
    private readonly WoongBlogDbContext _dbContext;

    public AuthAuditService(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordLoginFailureAsync(HttpContext httpContext, string reason, CancellationToken cancellationToken = default)
    {
        _dbContext.AuthAuditLogs.Add(new WoongBlog.Api.Domain.Entities.AuthAuditLog
        {
            EventType = "login_failure",
            Provider = "google",
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            Success = false,
            FailureReason = reason,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordLogoutAsync(ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        _dbContext.AuthAuditLogs.Add(new WoongBlog.Api.Domain.Entities.AuthAuditLog
        {
            ProfileId = TryGuid(principal.FindFirstValue(AuthClaimTypes.ProfileId)),
            EventType = "logout",
            Provider = "google",
            ProviderSubject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub") ?? string.Empty,
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email") ?? string.Empty,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            Success = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordDeniedAccessAsync(
        ClaimsPrincipal principal,
        HttpContext httpContext,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _dbContext.AuthAuditLogs.Add(new WoongBlog.Api.Domain.Entities.AuthAuditLog
        {
            ProfileId = TryGuid(principal.FindFirstValue(AuthClaimTypes.ProfileId)),
            EventType = "access_denied",
            Provider = "google",
            ProviderSubject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub") ?? string.Empty,
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email") ?? string.Empty,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            Success = false,
            FailureReason = reason,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid? TryGuid(string? value) => Guid.TryParse(value, out var result) ? result : null;
}
