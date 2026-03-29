using WoongBlog.Api.Application.Admin.GetAdminSiteSettings;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Application.Admin.UpdateSiteSettings;

namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminSiteSettingsService
{
    Task<AdminSiteSettingsDto?> GetAsync(CancellationToken cancellationToken);
    Task<AdminActionResult> UpdateAsync(UpdateSiteSettingsCommand command, CancellationToken cancellationToken);
}
