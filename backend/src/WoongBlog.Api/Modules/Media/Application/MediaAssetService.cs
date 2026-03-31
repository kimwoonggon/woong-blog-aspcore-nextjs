using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Modules.Media.Application;

public sealed class MediaAssetService : IMediaAssetService
{
    private readonly WoongBlogDbContext _dbContext;
    private readonly AuthOptions _authOptions;

    public MediaAssetService(WoongBlogDbContext dbContext, IOptions<AuthOptions> authOptions)
    {
        _dbContext = dbContext;
        _authOptions = authOptions.Value;
    }

    public async Task<MediaUploadResult> UploadAsync(
        IFormFile? file,
        string? bucket,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return new MediaUploadResult(false, StatusCodes.Status400BadRequest, "No file uploaded", null, null, null);
        }

        var normalizedBucket = string.IsNullOrWhiteSpace(bucket) ? "media" : bucket.Trim();
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(normalizedBucket, fileName).Replace('\\', '/');
        var physicalPath = Path.Combine(_authOptions.MediaRoot, relativePath);
        var directory = Path.GetDirectoryName(physicalPath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            return new MediaUploadResult(false, StatusCodes.Status500InternalServerError, "The upload path could not be resolved.", null, null, null);
        }

        Directory.CreateDirectory(directory);

        await using (var stream = File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var profileIdValue = user.FindFirst(AuthClaimTypes.ProfileId)?.Value;
        Guid? profileId = Guid.TryParse(profileIdValue, out var parsed) ? parsed : null;

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Bucket = normalizedBucket,
            Path = relativePath,
            PublicUrl = $"/media/{relativePath}",
            MimeType = file.ContentType,
            Size = file.Length,
            Kind = GetKind(file.ContentType),
            CreatedBy = profileId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Assets.Add(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MediaUploadResult(true, StatusCodes.Status200OK, null, asset.Id, asset.PublicUrl, asset.Path);
    }

    public async Task<MediaDeleteResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var asset = await _dbContext.Assets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (asset is null)
        {
            return new MediaDeleteResult(false);
        }

        var physicalPath = Path.Combine(_authOptions.MediaRoot, asset.Path);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        _dbContext.Assets.Remove(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new MediaDeleteResult(true);
    }

    private static string GetKind(string mimeType)
    {
        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "image";
        if (string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return "pdf";
        if (mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return "audio";
        return "other";
    }
}
