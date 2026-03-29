using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Infrastructure.Persistence.Assets;

public interface IAssetStorageService
{
    Task<AssetStoreResult> StoreAsync(IFormFile file, string? bucket, Guid? createdBy, CancellationToken cancellationToken);

    Task<AssetDeleteResult> DeleteAsync(Guid assetId, CancellationToken cancellationToken);
}
