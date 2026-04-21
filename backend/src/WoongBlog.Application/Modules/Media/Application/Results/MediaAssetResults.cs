namespace WoongBlog.Api.Modules.Media.Application.Results;

public enum MediaUploadStatus
{
    Success,
    BadRequest,
    Failed
}

public sealed record MediaUploadResult(
    bool Success,
    MediaUploadStatus Status,
    string? Error,
    Guid? AssetId,
    string? PublicUrl,
    string? Path);

public sealed record MediaDeleteResult(bool Found);
