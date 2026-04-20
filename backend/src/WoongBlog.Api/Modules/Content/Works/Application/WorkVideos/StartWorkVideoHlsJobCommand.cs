using MediatR;
using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed record StartWorkVideoHlsJobCommand(
    Guid WorkId,
    IFormFile? File,
    int ExpectedVideosVersion) : IRequest<WorkVideoResult<WorkVideosMutationResult>>;
