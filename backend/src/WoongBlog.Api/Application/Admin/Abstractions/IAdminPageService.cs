using WoongBlog.Api.Application.Admin.GetAdminPages;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminPageQueries
{
    Task<IReadOnlyList<AdminPageListItemDto>> GetPagesAsync(string[]? slugs, CancellationToken cancellationToken);
}

public interface IAdminPageWriteStore
{
    Task<PageEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
