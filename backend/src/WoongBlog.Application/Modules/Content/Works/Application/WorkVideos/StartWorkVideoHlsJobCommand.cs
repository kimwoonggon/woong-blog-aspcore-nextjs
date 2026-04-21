using MediatR;
using WoongBlog.Api.Common.Application.Files;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed record StartWorkVideoHlsJobCommand(
    Guid WorkId,
    IUploadedFile? File,
    int ExpectedVideosVersion) : IRequest<WorkVideoResult<WorkVideosMutationResult>>;
