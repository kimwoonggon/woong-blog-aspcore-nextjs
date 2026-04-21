using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;

public interface IPageCommandStore
{
    Task<PageEntity?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
