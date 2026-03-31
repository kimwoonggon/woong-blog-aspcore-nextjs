using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Site.Application.Abstractions;
using WoongBlog.Api.Modules.Site.Application.GetResume;
using WoongBlog.Api.Modules.Site.Application.GetSiteSettings;

namespace WoongBlog.Api.Modules.Site.Persistence;

public sealed class PublicSiteService : IPublicSiteService
{
    private readonly WoongBlogDbContext _dbContext;

    public PublicSiteService(WoongBlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SiteSettingsDto?> GetSiteSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.SiteSettings
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
        var siteSettings = await _dbContext.SiteSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Singleton, cancellationToken);

        if (siteSettings?.ResumeAssetId is null)
        {
            return null;
        }

        var asset = await _dbContext.Assets.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == siteSettings.ResumeAssetId.Value, cancellationToken);

        return asset is null ? null : new ResumeDto(asset.Id, asset.PublicUrl, System.IO.Path.GetFileName(asset.Path), asset.Path);
    }
}
