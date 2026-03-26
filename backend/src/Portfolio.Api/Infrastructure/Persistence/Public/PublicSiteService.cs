using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Application.Public.Abstractions;
using Portfolio.Api.Application.Public.GetResume;
using Portfolio.Api.Application.Public.GetSiteSettings;

namespace Portfolio.Api.Infrastructure.Persistence.Public;

public sealed class PublicSiteService : IPublicSiteService
{
    private readonly PortfolioDbContext _dbContext;

    public PublicSiteService(PortfolioDbContext dbContext)
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

        return asset is null ? null : new ResumeDto(asset.PublicUrl, asset.Path);
    }
}
