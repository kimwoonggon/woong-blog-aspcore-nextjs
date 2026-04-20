using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Infrastructure.Auth;

public class AuthRecorder
{
    private readonly WoongBlogDbContext _dbContext;
    private readonly AuthOptions _authOptions;

    public AuthRecorder(WoongBlogDbContext dbContext, Microsoft.Extensions.Options.IOptions<AuthOptions> authOptions)
    {
        _dbContext = dbContext;
        _authOptions = authOptions.Value;
    }

    public async Task<AuthRecordResult> RecordSuccessfulLoginAsync(
        ClaimsPrincipal principal,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var providerSubject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? principal.FindFirstValue("sub")
                              ?? throw new InvalidOperationException("Missing subject claim.");
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email")
                    ?? throw new InvalidOperationException("Missing email claim.");
        var displayName = principal.Identity?.Name
                          ?? principal.FindFirstValue("name")
                          ?? email;

        var now = DateTimeOffset.UtcNow;

        var profile = await _dbContext.Profiles.SingleOrDefaultAsync(
            x => x.Provider == "google" && x.ProviderSubject == providerSubject,
            cancellationToken);

        profile ??= await _dbContext.Profiles.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (profile is null)
        {
            profile = new Profile
            {
                Id = Guid.NewGuid(),
                Provider = "google",
                ProviderSubject = providerSubject,
                Email = email,
                DisplayName = displayName,
                Role = ResolveRole(email),
                CreatedAt = now,
                UpdatedAt = now,
                LastLoginAt = now
            };
            _dbContext.Profiles.Add(profile);
        }
        else
        {
            profile.Provider = "google";
            profile.ProviderSubject = providerSubject;
            profile.Email = email;
            profile.DisplayName = displayName;
            profile.LastLoginAt = now;
            profile.UpdatedAt = now;
            if (string.IsNullOrWhiteSpace(profile.Role))
            {
                profile.Role = ResolveRole(email);
            }
        }

        var session = new AuthSession
        {
            ProfileId = profile.Id,
            SessionKey = Guid.NewGuid().ToString("N"),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now.AddHours(_authOptions.AbsoluteExpirationHours)
        };
        _dbContext.AuthSessions.Add(session);

        _dbContext.AuthAuditLogs.Add(new AuthAuditLog
        {
            ProfileId = profile.Id,
            EventType = "login_success",
            Provider = "google",
            ProviderSubject = providerSubject,
            Email = email,
            IpAddress = session.IpAddress,
            UserAgent = session.UserAgent,
            Success = true,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AuthRecordResult(profile.Id, profile.Email, profile.Role, session.Id, profile.DisplayName);
    }

    public async Task RecordLoginFailureAsync(HttpContext httpContext, string reason, CancellationToken cancellationToken = default)
    {
        _dbContext.AuthAuditLogs.Add(new AuthAuditLog
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
        var sessionIdValue = principal.FindFirstValue(AuthClaimTypes.SessionId);
        if (Guid.TryParse(sessionIdValue, out var sessionId))
        {
            var session = await _dbContext.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
            if (session is not null)
            {
                session.RevokedAt = DateTimeOffset.UtcNow;
                session.LastSeenAt = DateTimeOffset.UtcNow;
            }
        }

        _dbContext.AuthAuditLogs.Add(new AuthAuditLog
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
        _dbContext.AuthAuditLogs.Add(new AuthAuditLog
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

    public async Task<bool> ValidateSessionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var sessionIdValue = principal.FindFirstValue(AuthClaimTypes.SessionId);
        if (!Guid.TryParse(sessionIdValue, out var sessionId))
        {
            return false;
        }

        var profileIdValue = principal.FindFirstValue(AuthClaimTypes.ProfileId);
        if (!Guid.TryParse(profileIdValue, out var profileId))
        {
            return false;
        }

        var session = await _dbContext.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (session is null || session.RevokedAt is not null)
        {
            return false;
        }

        if (session.ExpiresAt is not null && session.ExpiresAt <= now)
        {
            session.RevokedAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        if (session.LastSeenAt.AddMinutes(_authOptions.SlidingExpirationMinutes) <= now)
        {
            session.RevokedAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        var profile = await _dbContext.Profiles.SingleOrDefaultAsync(x => x.Id == profileId, cancellationToken);
        if (profile is null)
        {
            session.RevokedAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        var roleClaim = principal.FindFirstValue(AuthClaimTypes.Role) ?? principal.FindFirstValue(ClaimTypes.Role);
        if (!string.Equals(roleClaim, profile.Role, StringComparison.OrdinalIgnoreCase))
        {
            session.RevokedAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        if (ShouldRefreshLastSeen(session.LastSeenAt, now))
        {
            session.LastSeenAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.RevokedAt = DateTimeOffset.UtcNow;
        session.LastSeenAt = DateTimeOffset.UtcNow;

        _dbContext.AuthAuditLogs.Add(new AuthAuditLog
        {
            ProfileId = session.ProfileId,
            EventType = "session_revoked",
            Provider = "google",
            Success = true,
            FailureReason = reason,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private string ResolveRole(string email)
        => _authOptions.AdminEmails.Any(x => string.Equals(x, email, StringComparison.OrdinalIgnoreCase))
            ? "admin"
            : "user";

    private bool ShouldRefreshLastSeen(DateTimeOffset lastSeenAt, DateTimeOffset now)
    {
        var refreshIntervalSeconds = Math.Max(1, _authOptions.LastSeenRefreshIntervalSeconds);
        return lastSeenAt.AddSeconds(refreshIntervalSeconds) <= now;
    }

    private static Guid? TryGuid(string? value) => Guid.TryParse(value, out var result) ? result : null;
}
