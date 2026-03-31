using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Modules.Media.Application;

public interface IMediaAssetService
{
    Task<MediaUploadResult> UploadAsync(
        IFormFile? file,
        string? bucket,
        ClaimsPrincipal user,
        CancellationToken cancellationToken);

    Task<MediaDeleteResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record MediaUploadResult(
    bool Success,
    int StatusCode,
    string? Error,
    Guid? AssetId,
    string? PublicUrl,
    string? Path);

public sealed record MediaDeleteResult(bool Found);
