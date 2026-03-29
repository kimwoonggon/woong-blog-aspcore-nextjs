using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Auth;

namespace WoongBlog.Api.Infrastructure.Persistence.Assets;

public sealed class AssetStorageService : IAssetStorageService
{
    private static readonly HashSet<string> AllowedBuckets =
    [
        "media",
        "public-assets",
        "public-resume",
        "work-thumbnails",
        "work-icons"
    ];

    private readonly WoongBlogDbContext _dbContext;
    private readonly AuthOptions _authOptions;

    public AssetStorageService(WoongBlogDbContext dbContext, IOptions<AuthOptions> authOptions)
    {
        _dbContext = dbContext;
        _authOptions = authOptions.Value;
    }

    public async Task<AssetStoreResult> StoreAsync(IFormFile file, string? bucket, Guid? createdBy, CancellationToken cancellationToken)
    {
        var normalizedBucket = NormalizeBucket(bucket);
        if (!AllowedBuckets.Contains(normalizedBucket))
        {
            return new AssetStoreResult(null, new AssetStorageError(AssetStorageErrorKind.InvalidBucket, "Bucket is not allowed."));
        }

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(normalizedBucket, fileName).Replace('\\', '/');
        var physicalPath = ResolvePathWithinRoot(relativePath);
        if (physicalPath is null)
        {
            return new AssetStoreResult(null, new AssetStorageError(AssetStorageErrorKind.InvalidPath, "The upload path could not be resolved."));
        }

        var directory = Path.GetDirectoryName(physicalPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return new AssetStoreResult(null, new AssetStorageError(AssetStorageErrorKind.InvalidPath, "The upload path could not be resolved."));
        }

        Directory.CreateDirectory(directory);
        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Bucket = normalizedBucket,
            Path = relativePath,
            PublicUrl = $"/media/{relativePath}",
            MimeType = file.ContentType,
            Size = file.Length,
            Kind = GetKind(file.ContentType),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Assets.Add(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AssetStoreResult(asset, null);
    }

    public async Task<AssetDeleteResult> DeleteAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await _dbContext.Assets.SingleOrDefaultAsync(x => x.Id == assetId, cancellationToken);
        if (asset is null)
        {
            return new AssetDeleteResult(false, new AssetStorageError(AssetStorageErrorKind.AssetNotFound, "Asset not found"));
        }

        var physicalPath = ResolvePathWithinRoot(asset.Path);
        if (physicalPath is null)
        {
            return new AssetDeleteResult(false, new AssetStorageError(AssetStorageErrorKind.StoredPathInvalid, "Stored asset path is invalid."));
        }

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }

        _dbContext.Assets.Remove(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new AssetDeleteResult(true, null);
    }

    private string NormalizeBucket(string? bucket)
    {
        if (string.IsNullOrWhiteSpace(bucket))
        {
            return "media";
        }

        return bucket.Trim().ToLowerInvariant();
    }

    private string? ResolvePathWithinRoot(string relativePath)
    {
        var root = Path.GetFullPath(_authOptions.MediaRoot);
        var combined = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : $"{root}{Path.DirectorySeparatorChar}";

        return combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ? combined : null;
    }

    private static string GetKind(string mimeType)
    {
        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "image";
        if (string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return "pdf";
        if (mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return "audio";
        return "other";
    }
}
