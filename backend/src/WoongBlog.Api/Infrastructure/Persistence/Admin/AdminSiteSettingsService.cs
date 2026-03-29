using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.GetAdminSiteSettings;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Application.Admin.UpdateSiteSettings;

namespace WoongBlog.Api.Infrastructure.Persistence.Admin;

public sealed class AdminSiteSettingsService : IAdminSiteSettingsService
{
    private readonly WoongBlogDbContext _dbContext;

    public AdminSiteSettingsService(WoongBlogDbContext dbContext)
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

    public async Task<AdminActionResult> UpdateAsync(UpdateSiteSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await _dbContext.SiteSettings.SingleOrDefaultAsync(x => x.Singleton, cancellationToken);
        if (settings is null)
        {
            return new AdminActionResult(false);
        }

        if (command.OwnerName is not null) settings.OwnerName = command.OwnerName;
        if (command.Tagline is not null) settings.Tagline = command.Tagline;
        if (command.FacebookUrl is not null) settings.FacebookUrl = command.FacebookUrl;
        if (command.InstagramUrl is not null) settings.InstagramUrl = command.InstagramUrl;
        if (command.TwitterUrl is not null) settings.TwitterUrl = command.TwitterUrl;
        if (command.LinkedInUrl is not null) settings.LinkedInUrl = command.LinkedInUrl;
        if (command.GitHubUrl is not null) settings.GitHubUrl = command.GitHubUrl;
        if (command.HasResumeAssetId) settings.ResumeAssetId = command.ResumeAssetId;

        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
