using Portfolio.Api.Application.Public.GetResume;
using Portfolio.Api.Application.Public.GetSiteSettings;

namespace Portfolio.Api.Application.Public.Abstractions;

public interface IPublicSiteService
{
    Task<SiteSettingsDto?> GetSiteSettingsAsync(CancellationToken cancellationToken);
    Task<ResumeDto?> GetResumeAsync(CancellationToken cancellationToken);
}
