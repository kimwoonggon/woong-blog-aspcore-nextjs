using Microsoft.AspNetCore.Http;
using WoongBlog.Api.Modules.Media.Application.Abstractions;

namespace WoongBlog.Api.Modules.Media.Policies;

public sealed class MediaAssetUploadPolicy : IMediaAssetUploadPolicy
{
    private const long MaxImageBytes = 10L * 1024L * 1024L;
    private static readonly HashSet<string> AllowedImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/gif",
        "image/jpeg",
        "image/png",
        "image/svg+xml",
        "image/webp"
    };

    public string? Validate(IFormFile? file)
    {
        if (file is null)
        {
            return "No file uploaded";
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!AllowedImageMimeTypes.Contains(file.ContentType))
        {
            return "Unsupported image type.";
        }

        if (file.Length > MaxImageBytes)
        {
            return "Image uploads must be 10MB or smaller.";
        }

        return null;
    }

    public MediaUploadPlan BuildPlan(IFormFile file, string? bucket)
    {
        var normalizedBucket = string.IsNullOrWhiteSpace(bucket) ? "media" : bucket.Trim();
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(normalizedBucket, fileName).Replace('\\', '/');
        return new MediaUploadPlan(normalizedBucket, relativePath, $"/media/{relativePath}");
    }

    public string GetKind(string mimeType)
    {
        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "image";
        if (string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return "pdf";
        if (mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return "audio";
        return "other";
    }
}
