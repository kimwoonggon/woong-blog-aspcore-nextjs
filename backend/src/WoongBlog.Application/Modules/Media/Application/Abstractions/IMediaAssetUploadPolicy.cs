using WoongBlog.Api.Common.Application.Files;

namespace WoongBlog.Api.Modules.Media.Application.Abstractions;

public interface IMediaAssetUploadPolicy
{
    string? Validate(IUploadedFile? file);
    MediaUploadPlan BuildPlan(IUploadedFile file, string? bucket);
    string GetKind(string mimeType);
}

public sealed record MediaUploadPlan(string Bucket, string RelativePath, string PublicUrl);
