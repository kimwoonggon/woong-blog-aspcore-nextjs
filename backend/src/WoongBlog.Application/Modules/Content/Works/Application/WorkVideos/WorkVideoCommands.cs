using MediatR;
using WoongBlog.Api.Common.Application.Files;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed record IssueWorkVideoUploadCommand(
    Guid WorkId,
    string FileName,
    string ContentType,
    long Size,
    int ExpectedVideosVersion) : IRequest<WorkVideoResult<VideoUploadTargetResult>>;

public sealed record UploadLocalWorkVideoCommand(
    Guid WorkId,
    Guid UploadSessionId,
    IUploadedFile? File) : IRequest<WorkVideoResult<object>>;

public sealed record ConfirmWorkVideoUploadCommand(
    Guid WorkId,
    Guid UploadSessionId,
    int ExpectedVideosVersion) : IRequest<WorkVideoResult<WorkVideosMutationResult>>;

public sealed record AddYouTubeWorkVideoCommand(
    Guid WorkId,
    string YoutubeUrlOrId,
    int ExpectedVideosVersion) : IRequest<WorkVideoResult<WorkVideosMutationResult>>;

public sealed record ReorderWorkVideosCommand(
    Guid WorkId,
    IReadOnlyList<Guid> OrderedVideoIds,
    int ExpectedVideosVersion) : IRequest<WorkVideoResult<WorkVideosMutationResult>>;

public sealed record DeleteWorkVideoCommand(
    Guid WorkId,
    Guid VideoId,
    int ExpectedVideosVersion) : IRequest<WorkVideoResult<WorkVideosMutationResult>>;
