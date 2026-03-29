using WoongBlog.Api.Application.Admin.GetAdminWorkById;
using WoongBlog.Api.Application.Admin.GetAdminWorks;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminWorkQueries
{
    Task<IReadOnlyList<AdminWorkListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminWorkDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public interface IAdminWorkWriteStore
{
    Task<Work?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    void Add(Work work);
    void Remove(Work work);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
