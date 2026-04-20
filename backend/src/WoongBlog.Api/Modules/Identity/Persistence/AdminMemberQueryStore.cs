using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Identity.Application.Abstractions;
using WoongBlog.Api.Modules.Identity.Application.GetAdminMembers;

namespace WoongBlog.Api.Modules.Identity.Persistence;

public sealed class AdminMemberQueryStore(WoongBlogDbContext dbContext) : IAdminMemberQueryStore
{
    public async Task<IReadOnlyList<AdminMemberListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeSessions = await dbContext.AuthSessions
            .AsNoTracking()
            .Where(session => session.RevokedAt == null && (!session.ExpiresAt.HasValue || session.ExpiresAt > now))
            .ToListAsync(cancellationToken);

        var activeSessionCounts = activeSessions
            .GroupBy(session => session.ProfileId)
            .ToDictionary(group => group.Key, group => group.Count());

        var profiles = await dbContext.Profiles
            .AsNoTracking()
            .OrderByDescending(profile => profile.CreatedAt)
            .ToListAsync(cancellationToken);

        return profiles
            .Select(profile => new AdminMemberListItemDto(
                profile.Id,
                string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.Email : profile.DisplayName,
                profile.Email,
                profile.Role,
                profile.Provider,
                profile.CreatedAt,
                profile.LastLoginAt,
                activeSessionCounts.GetValueOrDefault(profile.Id)
            ))
            .ToList();
    }
}
