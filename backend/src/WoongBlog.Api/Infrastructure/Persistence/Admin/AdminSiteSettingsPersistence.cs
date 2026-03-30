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
        var siteSettings = await _dbContext.SiteSettings
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
                x.ResumeAssetId,
                null
            ))
            .SingleOrDefaultAsync(cancellationToken);

        if (siteSettings?.ResumeAssetId is null)
        {
            return siteSettings;
        }

        var resumeAsset = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.Id == siteSettings.ResumeAssetId.Value)
            .Select(x => new AdminResumeAssetDto(
                x.Id,
                x.Bucket,
                x.Path,
                x.PublicUrl,
                Path.GetFileName(x.Path)
            ))
            .SingleOrDefaultAsync(cancellationToken);

        return siteSettings with { ResumeAsset = resumeAsset };
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
