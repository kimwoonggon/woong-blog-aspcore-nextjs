using WoongBlog.Api.Modules.Site.Application.GetAdminSiteSettings;
using WoongBlog.Api.Modules.Site.Application.GetResume;
using WoongBlog.Api.Modules.Site.Application.GetSiteSettings;

namespace WoongBlog.Api.Modules.Site.Application.Abstractions;

public interface ISiteSettingsQueryStore
{
    Task<AdminSiteSettingsDto?> GetAdminSettingsAsync(CancellationToken cancellationToken);
    Task<SiteSettingsDto?> GetPublicSettingsAsync(CancellationToken cancellationToken);
    Task<ResumeDto?> GetResumeAsync(CancellationToken cancellationToken);
}
