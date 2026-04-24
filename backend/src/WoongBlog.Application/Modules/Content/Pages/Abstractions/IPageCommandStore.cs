using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Application.Modules.Content.Pages.Abstractions;

public interface IPageCommandStore
{
    Task<PageEntity?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
