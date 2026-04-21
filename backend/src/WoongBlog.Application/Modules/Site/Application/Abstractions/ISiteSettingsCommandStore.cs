using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Site.Application.Abstractions;

public interface ISiteSettingsCommandStore
{
    Task<SiteSetting?> GetForUpdateAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
