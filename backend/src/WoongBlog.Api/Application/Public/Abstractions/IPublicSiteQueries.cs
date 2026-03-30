using WoongBlog.Api.Application.Public.GetResume;
using WoongBlog.Api.Application.Public.GetSiteSettings;

namespace WoongBlog.Api.Application.Public.Abstractions;

public interface IPublicSiteQueries
{
    Task<SiteSettingsDto?> GetSiteSettingsAsync(CancellationToken cancellationToken);
    Task<ResumeDto?> GetResumeAsync(CancellationToken cancellationToken);
}
