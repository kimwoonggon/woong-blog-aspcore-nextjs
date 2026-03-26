using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Application.Admin.Abstractions;
using Portfolio.Api.Application.Admin.GetAdminMembers;

namespace Portfolio.Api.Infrastructure.Persistence.Admin;

public sealed class AdminMemberService : IAdminMemberService
{
    private readonly PortfolioDbContext _dbContext;

    public AdminMemberService(PortfolioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AdminMemberListItemDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeSessions = await _dbContext.AuthSessions
            .AsNoTracking()
            .Where(session => session.RevokedAt == null && (!session.ExpiresAt.HasValue || session.ExpiresAt > now))
            .ToListAsync(cancellationToken);

        var activeSessionCounts = activeSessions
            .GroupBy(session => session.ProfileId)
            .ToDictionary(group => group.Key, group => group.Count());

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
