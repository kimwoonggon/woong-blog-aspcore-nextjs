using Microsoft.EntityFrameworkCore;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Site.Abstractions;
using WoongBlog.Application.Modules.Site.GetAdminSiteSettings;
using WoongBlog.Application.Modules.Site.GetResume;
using WoongBlog.Application.Modules.Site.GetSiteSettings;

namespace WoongBlog.Infrastructure.Modules.Site.Persistence;

public sealed class SiteSettingsQueryStore(WoongBlogDbContext dbContext) : ISiteSettingsQueryStore
{
    public async Task<AdminSiteSettingsDto?> GetAdminSettingsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.SiteSettings
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

    public async Task<SiteSettingsDto?> GetPublicSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.SiteSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Singleton, cancellationToken);

        return settings is null
            ? null
            : new SiteSettingsDto(
                settings.OwnerName,
                settings.Tagline,
                settings.FacebookUrl,
                settings.InstagramUrl,
                settings.TwitterUrl,
                settings.LinkedInUrl,
                settings.GitHubUrl
            );
    }

    public async Task<ResumeDto?> GetResumeAsync(CancellationToken cancellationToken)
    {
        var siteSettings = await dbContext.SiteSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Singleton, cancellationToken);

        if (siteSettings?.ResumeAssetId is null)
        {
            return null;
        }

        var asset = await dbContext.Assets
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == siteSettings.ResumeAssetId.Value, cancellationToken);

        return asset is null ? null : new ResumeDto(asset.Id, asset.PublicUrl, Path.GetFileName(asset.Path), asset.Path);
    }
}
