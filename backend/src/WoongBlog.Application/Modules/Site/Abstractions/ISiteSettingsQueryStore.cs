using WoongBlog.Application.Modules.Site.GetAdminSiteSettings;
using WoongBlog.Application.Modules.Site.GetResume;
using WoongBlog.Application.Modules.Site.GetSiteSettings;

namespace WoongBlog.Application.Modules.Site.Abstractions;

public interface ISiteSettingsQueryStore
{
    Task<AdminSiteSettingsDto?> GetAdminSettingsAsync(CancellationToken cancellationToken);
    Task<SiteSettingsDto?> GetPublicSettingsAsync(CancellationToken cancellationToken);
    Task<ResumeDto?> GetResumeAsync(CancellationToken cancellationToken);
}
