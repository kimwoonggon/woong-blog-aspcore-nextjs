using Microsoft.EntityFrameworkCore;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Content.Pages.Abstractions;

namespace WoongBlog.Infrastructure.Modules.Content.Pages.Persistence;

public sealed class PageCommandStore(WoongBlogDbContext dbContext) : IPageCommandStore
{
    public Task<PageEntity?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Pages.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
