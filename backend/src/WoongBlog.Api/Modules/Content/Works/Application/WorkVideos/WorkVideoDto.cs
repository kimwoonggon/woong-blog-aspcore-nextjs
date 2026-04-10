using Microsoft.AspNetCore.Http;
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

public sealed record WorkVideoServiceResult<T>(
    int StatusCode,
    string? Error,
    T? Value)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;

    public static WorkVideoServiceResult<T> Ok(T value) => new(StatusCodes.Status200OK, null, value);
    public static WorkVideoServiceResult<T> BadRequest(string error) => new(StatusCodes.Status400BadRequest, error, default);
    public static WorkVideoServiceResult<T> NotFound(string error) => new(StatusCodes.Status404NotFound, error, default);
    public static WorkVideoServiceResult<T> Conflict(string error) => new(StatusCodes.Status409Conflict, error, default);
    public static WorkVideoServiceResult<T> Unsupported(string error) => new(StatusCodes.Status400BadRequest, error, default);
}
