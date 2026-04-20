using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Site.Application.Abstractions;

namespace WoongBlog.Api.Modules.Site.Persistence;

public sealed class SiteSettingsCommandStore(WoongBlogDbContext dbContext) : ISiteSettingsCommandStore
{
    public Task<SiteSetting?> GetForUpdateAsync(CancellationToken cancellationToken)
    {
        return dbContext.SiteSettings.SingleOrDefaultAsync(x => x.Singleton, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
