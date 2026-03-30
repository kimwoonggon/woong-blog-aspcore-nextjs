using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.GetAdminSiteSettings;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Infrastructure.Persistence.Admin;

public sealed class AdminSiteSettingsPersistence : IAdminSiteSettingsQueries, IAdminSiteSettingsWriteStore
{
    private readonly WoongBlogDbContext _dbContext;

    public AdminSiteSettingsPersistence(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminSiteSettingsDto?> GetAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.SiteSettings
            .AsNoTracking()
            .Where(x => x.Singleton)
            .Select(x => new AdminSiteSettingsDto(
                x.OwnerName,
                x.Tagline,
                x.FacebookUrl,
                x.InstagramUrl,
                x.TwitterUrl,
                x.LinkedInUrl,
                x.GitHubUrl,
                x.ResumeAssetId
            ))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<SiteSetting?> GetSingletonAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SiteSettings.SingleOrDefaultAsync(x => x.Singleton, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
