using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Infrastructure.Auth;

public sealed class AuthSessionService : IAuthSessionService
{
    private static readonly TimeSpan LastSeenUpdateInterval = TimeSpan.FromMinutes(5);

    private readonly WoongBlogDbContext _dbContext;
    private readonly AuthOptions _authOptions;

    public AuthSessionService(WoongBlogDbContext dbContext, Microsoft.Extensions.Options.IOptions<AuthOptions> authOptions)
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
        if (session is null || session.RevokedAt is not null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (session.ExpiresAt is not null && session.ExpiresAt <= now)
        {
            await RevokeIfActiveAsync(session, now, cancellationToken);
            return false;
        }

        if (session.LastSeenAt.AddMinutes(_authOptions.SlidingExpirationMinutes) <= now)
        {
            await RevokeIfActiveAsync(session, now, cancellationToken);
            return false;
        }

        var profile = await _dbContext.Profiles.SingleOrDefaultAsync(x => x.Id == profileId, cancellationToken);
        if (profile is null)
        {
            await RevokeIfActiveAsync(session, now, cancellationToken);
            return false;
        }

        var roleClaim = principal.FindFirstValue(AuthClaimTypes.Role) ?? principal.FindFirstValue(ClaimTypes.Role);
        if (!string.Equals(roleClaim, profile.Role, StringComparison.OrdinalIgnoreCase))
        {
            await RevokeIfActiveAsync(session, now, cancellationToken);
            return false;
        }

        if (now - session.LastSeenAt >= LastSeenUpdateInterval)
        {
            session.LastSeenAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task RevokeCurrentSessionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var sessionIdValue = principal.FindFirstValue(AuthClaimTypes.SessionId);
        if (!Guid.TryParse(sessionIdValue, out var sessionId))
        {
            return;
        }

        await RevokeSessionAsync(sessionId, "logout", cancellationToken);
    }

    public async Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null || session.RevokedAt is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        session.RevokedAt = now;
        session.LastSeenAt = now;

        _dbContext.AuthAuditLogs.Add(new AuthAuditLog
        {
            ProfileId = session.ProfileId,
            EventType = "session_revoked",
            Provider = "google",
            Success = true,
            FailureReason = reason,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RevokeIfActiveAsync(AuthSession session, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (session.RevokedAt is not null)
        {
            return;
        }

        session.RevokedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private string ResolveRole(string email)
        => _authOptions.AdminEmails.Any(x => string.Equals(x, email, StringComparison.OrdinalIgnoreCase))
            ? "admin"
            : "user";
}
