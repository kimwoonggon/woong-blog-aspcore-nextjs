using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Application.Modules.Site.Abstractions;

public interface ISiteSettingsCommandStore
{
    Task<SiteSetting?> GetForUpdateAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
