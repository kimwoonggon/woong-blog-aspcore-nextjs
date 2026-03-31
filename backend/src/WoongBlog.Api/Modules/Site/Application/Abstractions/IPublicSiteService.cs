using WoongBlog.Api.Modules.Site.Application.GetResume;
using WoongBlog.Api.Modules.Site.Application.GetSiteSettings;

namespace WoongBlog.Api.Modules.Site.Application.Abstractions;

public interface IPublicSiteService
{
    Task<SiteSettingsDto?> GetSiteSettingsAsync(CancellationToken cancellationToken);
    Task<ResumeDto?> GetResumeAsync(CancellationToken cancellationToken);
}
