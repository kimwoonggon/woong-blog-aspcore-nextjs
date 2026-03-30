using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Infrastructure.Persistence.Admin;

namespace WoongBlog.Api.Tests;

public class AdminMemberQueriesTests
{
    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_CountsOnlyActiveSessions()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var profileA = Profile.Seed(Guid.NewGuid(), "a@example.com", "Alice", "google", "subject-a", "admin", now.AddDays(-2), now.AddDays(-1), now);
        var profileB = Profile.Seed(Guid.NewGuid(), "b@example.com", "Bob", "google", "subject-b", "user", now.AddDays(-1), now, now);

        dbContext.Profiles.AddRange(profileA, profileB);
        dbContext.AuthSessions.AddRange(
            AuthSession.Seed(Guid.NewGuid(), profileA.Id, "active-a-1", now.AddMinutes(-30), now.AddMinutes(-5), now.AddHours(1)),
            AuthSession.Seed(Guid.NewGuid(), profileA.Id, "active-a-2", now.AddMinutes(-20), now.AddMinutes(-2), null),
            AuthSession.Seed(Guid.NewGuid(), profileA.Id, "revoked-a", now.AddMinutes(-15), now.AddMinutes(-1), now.AddHours(1), revokedAt: now.AddMinutes(-1)),
            AuthSession.Seed(Guid.NewGuid(), profileB.Id, "expired-b", now.AddHours(-2), now.AddHours(-1), now.AddMinutes(-10))
        );
        await dbContext.SaveChangesAsync();

        var queries = new AdminMemberQueries(dbContext);

        var result = await queries.GetAllAsync(CancellationToken.None);

        var alice = Assert.Single(result.Where(x => x.Id == profileA.Id));
        var bob = Assert.Single(result.Where(x => x.Id == profileB.Id));
        Assert.Equal(2, alice.ActiveSessionCount);
        Assert.Equal(0, bob.ActiveSessionCount);
    }
}
