using Portfolio.Api.Application.Admin.GetAdminSiteSettings;
using Portfolio.Api.Application.Admin.Support;
using Portfolio.Api.Application.Admin.UpdateSiteSettings;

namespace Portfolio.Api.Application.Admin.Abstractions;

public interface IAdminSiteSettingsService
{
    Task<AdminSiteSettingsDto?> GetAsync(CancellationToken cancellationToken);
    Task<AdminActionResult> UpdateAsync(UpdateSiteSettingsCommand command, CancellationToken cancellationToken);
}
