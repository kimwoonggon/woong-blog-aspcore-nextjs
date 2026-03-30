using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Infrastructure.Auth;

public sealed class AuthProfileLookupService : IAuthProfileLookupService
{
    private readonly WoongBlogDbContext _dbContext;

    public AuthProfileLookupService(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthProfileLookupResult?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Profiles
            .AsNoTracking()
            .Where(x => x.Email == email)
            .Select(x => new AuthProfileLookupResult(
                x.Id,
                x.Email,
                x.DisplayName,
                x.ProviderSubject))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
