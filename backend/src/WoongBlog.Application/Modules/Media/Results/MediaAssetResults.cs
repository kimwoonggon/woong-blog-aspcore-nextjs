namespace WoongBlog.Application.Modules.Media.Results;

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
