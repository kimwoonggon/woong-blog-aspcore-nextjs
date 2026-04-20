namespace WoongBlog.Api.Modules.Media.Application.Results;

public sealed record MediaUploadResult(
    bool Success,
    int StatusCode,
    string? Error,
    Guid? AssetId,
    string? PublicUrl,
    string? Path);

public sealed record MediaDeleteResult(bool Found);
