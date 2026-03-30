using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

public class AuthSessionServiceTests
{
    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    private static AuthSessionService CreateSessionService(WoongBlogDbContext dbContext, AuthOptions? options = null)
        => new(dbContext, Options.Create(options ?? new AuthOptions()));

    private static AuthAuditService CreateAuditService(WoongBlogDbContext dbContext)
        => new(dbContext);

    private static WoongBlog.Api.Domain.Entities.Profile SeedProfile(string email, string providerSubject, string role = "user")
        => WoongBlog.Api.Domain.Entities.Profile.Seed(Guid.NewGuid(), email, email, "google", providerSubject, role);

    private static WoongBlog.Api.Domain.Entities.AuthSession SeedSession(Guid profileId, string sessionKey, DateTimeOffset lastSeenAt, DateTimeOffset? expiresAt, DateTimeOffset? revokedAt = null)
        => WoongBlog.Api.Domain.Entities.AuthSession.Seed(Guid.NewGuid(), profileId, sessionKey, lastSeenAt, lastSeenAt, expiresAt, revokedAt);

    [Fact]
    public async Task RecordSuccessfulLogin_CreatesProfileSessionAndAuditLog()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSessionService(dbContext, new AuthOptions
        {
            AdminEmails = ["admin@example.com"]
        });

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "google-subject-1"),
            new Claim(ClaimTypes.Email, "admin@example.com"),
            new Claim("name", "Admin Example")
        ], "test"));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "xunit";

        var result = await service.RecordSuccessfulLoginAsync(principal, httpContext);

        Assert.Equal("admin", result.Role);
        Assert.Single(dbContext.Profiles);
        Assert.Single(dbContext.AuthSessions);
        Assert.Single(dbContext.AuthAuditLogs);
    }

    [Fact]
    public async Task RecordSuccessfulLogin_KeepsUserRole_WhenNoAdminOverrideOrMatchingEmailExists()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSessionService(dbContext, new AuthOptions
        {
            AdminEmails = ["admin@example.com"]
        });

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "google-subject-user"),
            new Claim(ClaimTypes.Email, "someone@example.com"),
            new Claim("name", "Someone")
        ], "test"));

        var httpContext = new DefaultHttpContext();

        var result = await service.RecordSuccessfulLoginAsync(principal, httpContext);

        Assert.Equal("user", result.Role);
        Assert.Equal("user", dbContext.Profiles.Single().Role);
    }

    [Fact]
    public async Task RevokeCurrentSession_AndRecordLogout_AddsAuditLog()
    {
        await using var dbContext = CreateDbContext();
        var sessionService = CreateSessionService(dbContext);
        var auditService = CreateAuditService(dbContext);

        var loginPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "google-subject-2"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim("name", "User Example")
        ], "test"));

        var httpContext = new DefaultHttpContext();
        var loginResult = await sessionService.RecordSuccessfulLoginAsync(loginPrincipal, httpContext);

        var logoutPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, loginResult.ProfileId.ToString()),
            new Claim(AuthClaimTypes.SessionId, loginResult.SessionId.ToString()),
            new Claim(ClaimTypes.Email, loginResult.Email)
        ], "cookie"));

        await sessionService.RevokeCurrentSessionAsync(logoutPrincipal);
        await auditService.RecordLogoutAsync(logoutPrincipal, httpContext);

        Assert.Equal(3, dbContext.AuthAuditLogs.Count());
        Assert.NotNull(dbContext.AuthSessions.Single().RevokedAt);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsFalse_WhenClaimsMissing()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSessionService(dbContext);

        var principal = new ClaimsPrincipal(new ClaimsIdentity([], "cookie"));

        var isValid = await service.ValidateSessionAsync(principal);

        Assert.False(isValid);
        Assert.Empty(dbContext.AuthSessions);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsFalse_WhenSessionRevoked_WithoutExtraWrite()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSessionService(dbContext);

        var profile = SeedProfile("user@example.com", "subject-revoked");
        var revokedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var lastSeenAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var session = SeedSession(profile.Id, "revoked", lastSeenAt, DateTimeOffset.UtcNow.AddHours(1), revokedAt);
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "user")
        ], "cookie"));

        var isValid = await service.ValidateSessionAsync(principal);
        var saved = dbContext.AuthSessions.Single();

        Assert.False(isValid);
        Assert.Equal(revokedAt, saved.RevokedAt);
        Assert.Equal(lastSeenAt, saved.LastSeenAt);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsTrue_WithoutUpdatingLastSeen_WhenRecentlySeen()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSessionService(dbContext);

        var profile = SeedProfile("user@example.com", "subject-valid");
        var lastSeenAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var session = SeedSession(profile.Id, "valid", lastSeenAt, DateTimeOffset.UtcNow.AddHours(1));
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "user")
        ], "cookie"));

        var isValid = await service.ValidateSessionAsync(principal);

        Assert.True(isValid);
        Assert.Equal(lastSeenAt, dbContext.AuthSessions.Single().LastSeenAt);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsTrue_AndUpdatesLastSeen_WhenThresholdExceeded()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSessionService(dbContext);

        var profile = SeedProfile("user@example.com", "subject-throttled");
        var lastSeenAt = DateTimeOffset.UtcNow.AddMinutes(-6);
        var session = SeedSession(profile.Id, "valid-throttled", lastSeenAt, DateTimeOffset.UtcNow.AddHours(1));
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "user")
        ], "cookie"));

        var isValid = await service.ValidateSessionAsync(principal);

        Assert.True(isValid);
        Assert.True(dbContext.AuthSessions.Single().LastSeenAt > lastSeenAt);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsFalse_AndRevokesSession_WhenSessionExpired()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSessionService(dbContext, new AuthOptions
        {
            SlidingExpirationMinutes = 1,
            AbsoluteExpirationHours = 1
        });

        var profile = SeedProfile("user@example.com", "subject-expired");
        var session = SeedSession(profile.Id, "expired", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "user")
        ], "cookie"));

        var isValid = await service.ValidateSessionAsync(principal);

        Assert.False(isValid);
        Assert.NotNull(dbContext.AuthSessions.Single().RevokedAt);
    }

    [Fact]
    public async Task ValidateSessionAsync_ReturnsFalse_AndRevokes_WhenRoleDrifts()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSessionService(dbContext);

        var profile = SeedProfile("user@example.com", "subject-drift");
        var session = SeedSession(profile.Id, "role-drift", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        dbContext.Profiles.Add(profile);
        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.ProfileId, profile.Id.ToString()),
            new Claim(AuthClaimTypes.SessionId, session.Id.ToString()),
            new Claim(AuthClaimTypes.Role, "admin")
        ], "cookie"));

        var isValid = await service.ValidateSessionAsync(principal);

        Assert.False(isValid);
        Assert.NotNull(dbContext.AuthSessions.Single().RevokedAt);
    }
}
