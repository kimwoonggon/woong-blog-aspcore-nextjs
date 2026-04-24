using MediatR;
using WoongBlog.Api.Common.Application.Files;

namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public sealed record StartWorkVideoHlsJobCommand(
    Guid WorkId,
    IUploadedFile? File,
    int ExpectedVideosVersion) : IRequest<WorkVideoResult<WorkVideosMutationResult>>;
