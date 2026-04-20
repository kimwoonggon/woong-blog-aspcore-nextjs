using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Modules.Media.Application.Abstractions;

public interface IMediaAssetUploadPolicy
{
    string? Validate(IFormFile? file);
    MediaUploadPlan BuildPlan(IFormFile file, string? bucket);
    string GetKind(string mimeType);
}

public sealed record MediaUploadPlan(string Bucket, string RelativePath, string PublicUrl);
