using System.Text.Json.Serialization;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed record WorkVideoDto(
    Guid Id,
    string SourceType,
    string SourceKey,
    string? PlaybackUrl,
    string? OriginalFileName,
    string? MimeType,
    long? FileSize,
    int SortOrder,
    DateTimeOffset CreatedAt
);

public sealed record WorkVideosMutationResult(
    [property: JsonPropertyName("videos_version")] int VideosVersion,
    IReadOnlyList<WorkVideoDto> Videos
);

public sealed record VideoUploadTargetResult(
    Guid UploadSessionId,
    string UploadMethod,
    string UploadUrl,
    string StorageKey
);

public enum WorkVideoResultStatus
{
    Success,
    BadRequest,
    NotFound,
    Conflict,
    Unsupported
}

public sealed record WorkVideoResult<T>(
    WorkVideoResultStatus Status,
    string? Error,
    T? Value)
{
    public bool IsSuccess => Status == WorkVideoResultStatus.Success;

    public static WorkVideoResult<T> Ok(T value) => new(WorkVideoResultStatus.Success, null, value);
    public static WorkVideoResult<T> BadRequest(string error) => new(WorkVideoResultStatus.BadRequest, error, default);
    public static WorkVideoResult<T> NotFound(string error) => new(WorkVideoResultStatus.NotFound, error, default);
    public static WorkVideoResult<T> Conflict(string error) => new(WorkVideoResultStatus.Conflict, error, default);
    public static WorkVideoResult<T> Unsupported(string error) => new(WorkVideoResultStatus.Unsupported, error, default);
}
