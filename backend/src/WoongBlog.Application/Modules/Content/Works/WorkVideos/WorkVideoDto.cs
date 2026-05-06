using System.Text.Json.Serialization;

namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public sealed record WorkVideoDto(
    Guid Id,
    string SourceType,
    string SourceKey,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PlaybackUrl,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? OriginalFileName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MimeType,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? FileSize,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Width,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Height,
    [property: JsonPropertyName("duration_seconds")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    double? DurationSeconds,
    [property: JsonPropertyName("timeline_preview_vtt_url")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? TimelinePreviewVttUrl,
    [property: JsonPropertyName("timeline_preview_sprite_url")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? TimelinePreviewSpriteUrl,
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
