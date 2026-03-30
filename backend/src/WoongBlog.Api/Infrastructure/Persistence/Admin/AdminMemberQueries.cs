using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.GetAdminMembers;

namespace WoongBlog.Api.Infrastructure.Persistence.Admin;

public sealed class AdminMemberQueries : IAdminMemberQueries
{
    private readonly WoongBlogDbContext _dbContext;

    public AdminMemberQueries(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AdminMemberListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeSessionCounts = await _dbContext.AuthSessions
            .AsNoTracking()
            .Where(session => session.RevokedAt == null && (!session.ExpiresAt.HasValue || session.ExpiresAt > now))
            .GroupBy(session => session.ProfileId)
            .Select(group => new
            {
                ProfileId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(group => group.ProfileId, group => group.Count, cancellationToken);

        var profiles = await _dbContext.Profiles
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
