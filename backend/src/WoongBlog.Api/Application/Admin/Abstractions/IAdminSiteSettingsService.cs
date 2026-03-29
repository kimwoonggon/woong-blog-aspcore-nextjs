using WoongBlog.Api.Application.Admin.GetAdminSiteSettings;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminSiteSettingsQueries
{
    Task<AdminSiteSettingsDto?> GetAsync(CancellationToken cancellationToken);
}

public interface IAdminSiteSettingsWriteStore
{
    Task<SiteSetting?> GetSingletonAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
