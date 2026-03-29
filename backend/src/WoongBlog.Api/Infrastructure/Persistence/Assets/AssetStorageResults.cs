using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Infrastructure.Persistence.Assets;

public enum AssetStorageErrorKind
{
    InvalidBucket,
    InvalidPath,
    AssetNotFound,
    StoredPathInvalid
}

public sealed record AssetStorageError(AssetStorageErrorKind Kind, string Message);

public sealed record AssetStoreResult(Asset? Asset, AssetStorageError? Error)
{
    public bool IsSuccess => Asset is not null && Error is null;
}

public sealed record AssetDeleteResult(bool Deleted, AssetStorageError? Error)
{
    public bool IsSuccess => Deleted && Error is null;
}
