using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Media.Application.Abstractions;

public interface IMediaAssetCommandStore
{
    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Asset asset);
    void Remove(Asset asset);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
