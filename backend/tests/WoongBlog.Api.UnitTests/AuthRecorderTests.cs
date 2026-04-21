using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Identity.Application;

namespace WoongBlog.Api.Tests;

public class AuthRecorderTests
{
    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    [Fact]
    public async Task RecordSuccessfulLogin_CreatesProfileSessionAndAuditLog()
    {
        await using var dbContext = CreateDbContext();
        var recorder = new AuthRecorder(
            dbContext,
            Options.Create(new AuthOptions
            {
                AdminEmails = ["admin@example.com"]
            }));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "google-subject-1"),
            new Claim(ClaimTypes.Email, "admin@example.com"),
            new Claim("name", "Admin Example")
        ], "test"));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "xunit";

        var result = await recorder.RecordSuccessfulLoginAsync(principal, httpContext);

        Assert.Equal("admin", result.Role);
        Assert.Single(dbContext.Profiles);
        Assert.Single(dbContext.AuthSessions);
        Assert.Single(dbContext.AuthAuditLogs);
    }

    [Fact]
    public async Task RecordSuccessfulLogin_KeepsUserRole_WhenNoAdminOverrideOrMatchingEmailExists()
    {
        await using var dbContext = CreateDbContext();
        var recorder = new AuthRecorder(
            dbContext,
            Options.Create(new AuthOptions
            {
                AdminEmails = ["admin@example.com"]
            }));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "google-subject-user"),
            new Claim(ClaimTypes.Email, "someone@example.com"),
            new Claim("name", "Someone")
        ], "test"));

        var httpContext = new DefaultHttpContext();

        var result = await recorder.RecordSuccessfulLoginAsync(principal, httpContext);

        Assert.Equal("user", result.Role);
        Assert.Equal("user", dbContext.Profiles.Single().Role);
    }

    [Fact]
    public async Task RecordLogout_RevokesSessionAndAddsAuditLog()
    {
        await using var dbContext = CreateDbContext();
        var recorder = new AuthRecorder(dbContext, Options.Create(new AuthOptions()));

        var loginPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "google-subject-2"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim("name", "User Example")
        ], "test"));

        var httpContext = new DefaultHttpContext();
        var loginResult = await recorder.RecordSuccessfulLoginAsync(loginPrincipal, httpContext);

        var logoutPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, loginResult.ProfileId.ToString()),
            new Claim(AuthClaimTypes.SessionId, loginResult.SessionId.ToString()),
            new Claim(ClaimTypes.Email, loginResult.Email)
        ], "cookie"));

        await recorder.RecordLogoutAsync(logoutPrincipal, httpContext);

        Assert.Equal(2, dbContext.AuthAuditLogs.Count());
        Assert.NotNull(dbContext.AuthSessions.Single().RevokedAt);
    }


    [Fact]
    public async Task ValidateSessionAsync_ReturnsFalse_WhenClaimsMissing()
    {
        await using var dbContext = CreateDbContext();
        var recorder = new AuthRecorder(dbContext, Options.Create(new AuthOptions()));

        var principal = new ClaimsPrincipal(new ClaimsIdentity([], "cookie"));

        var isValid = await recorder.ValidateSessionAsync(principal);

        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsFalse_WhenSessionRevoked()
    {
        await using var dbContext = CreateDbContext();
        var recorder = new AuthRecorder(dbContext, Options.Create(new AuthOptions()));

        var profile = new WoongBlog.Api.Domain.Entities.Profile
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Provider = "google",
            ProviderSubject = "subject-revoked",
            Role = "user"
        };
        var session = new WoongBlog.Api.Domain.Entities.AuthSession
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            SessionKey = "revoked",
            LastSeenAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RevokedAt = DateTimeOffset.UtcNow
        };
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "user")
        ], "cookie"));

        var isValid = await recorder.ValidateSessionAsync(principal);

        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsTrue_AndUpdatesLastSeen_WhenSessionValid()
    {
        await using var dbContext = CreateDbContext();
        var recorder = new AuthRecorder(
            dbContext,
            Options.Create(new AuthOptions
            {
                LastSeenRefreshIntervalSeconds = 30
            }));

        var profile = new WoongBlog.Api.Domain.Entities.Profile
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Provider = "google",
            ProviderSubject = "subject-valid",
            Role = "user"
        };
        var lastSeenAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var session = new WoongBlog.Api.Domain.Entities.AuthSession
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            SessionKey = "valid",
            LastSeenAt = lastSeenAt,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "user")
        ], "cookie"));

        var isValid = await recorder.ValidateSessionAsync(principal);

        Assert.True(isValid);
        Assert.True(dbContext.AuthSessions.Single().LastSeenAt > lastSeenAt);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsTrue_WithoutUpdatingLastSeen_WhenSessionRecentlySeen()
    {
        await using var dbContext = CreateDbContext();
        var recorder = new AuthRecorder(
            dbContext,
            Options.Create(new AuthOptions
            {
                LastSeenRefreshIntervalSeconds = 30
            }));

        var profile = new WoongBlog.Api.Domain.Entities.Profile
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Provider = "google",
            ProviderSubject = "subject-valid",
            Role = "user"
        };
        var lastSeenAt = DateTimeOffset.UtcNow.AddSeconds(-5);
        var session = new WoongBlog.Api.Domain.Entities.AuthSession
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            SessionKey = "recent-valid",
            LastSeenAt = lastSeenAt,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "user")
        ], "cookie"));

        var isValid = await recorder.ValidateSessionAsync(principal);

        Assert.True(isValid);
        Assert.Equal(lastSeenAt, dbContext.AuthSessions.Single().LastSeenAt);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsFalse_WhenSessionExpired()
    {
        await using var dbContext = CreateDbContext();
        var recorder = new AuthRecorder(
            dbContext,
            Options.Create(new AuthOptions
            {
                SlidingExpirationMinutes = 1,
                AbsoluteExpirationHours = 1
            }));

        var profile = new WoongBlog.Api.Domain.Entities.Profile
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Provider = "google",
            ProviderSubject = "subject-expired",
            Role = "user"
        };
        var session = new WoongBlog.Api.Domain.Entities.AuthSession
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            SessionKey = "expired",
            LastSeenAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "user")
        ], "cookie"));

        var isValid = await recorder.ValidateSessionAsync(principal);

        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsFalse_WhenRoleDrifts()
    {
        await using var dbContext = CreateDbContext();
        var recorder = new AuthRecorder(dbContext, Options.Create(new AuthOptions()));

        var profile = new WoongBlog.Api.Domain.Entities.Profile
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Provider = "google",
            ProviderSubject = "subject-drift",
            Role = "user"
        };
        var session = new WoongBlog.Api.Domain.Entities.AuthSession
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            SessionKey = "role-drift",
            LastSeenAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "admin")
        ], "cookie"));

        var isValid = await recorder.ValidateSessionAsync(principal);

        Assert.False(isValid);
    }
}
