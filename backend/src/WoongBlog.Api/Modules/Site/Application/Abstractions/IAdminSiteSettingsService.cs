using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Site.Application.GetAdminSiteSettings;
using WoongBlog.Api.Modules.Site.Application.UpdateSiteSettings;

namespace WoongBlog.Api.Modules.Site.Application.Abstractions;

public interface IAdminSiteSettingsService
{
    Task<AdminSiteSettingsDto?> GetAsync(CancellationToken cancellationToken);
    Task<AdminActionResult> UpdateAsync(UpdateSiteSettingsCommand command, CancellationToken cancellationToken);
}
