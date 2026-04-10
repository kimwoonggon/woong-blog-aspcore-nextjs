using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Application.DeleteWork;

public sealed class DeleteWorkCommandHandler : IRequestHandler<DeleteWorkCommand, AdminActionResult>
{
    private readonly IAdminWorkService _adminWorkService;
    private readonly IWorkVideoService _workVideoService;

    public DeleteWorkCommandHandler(IAdminWorkService adminWorkService, IWorkVideoService workVideoService)
    {
        _adminWorkService = adminWorkService;
        _workVideoService = workVideoService;
    }

    public async Task<AdminActionResult> Handle(DeleteWorkCommand request, CancellationToken cancellationToken)
    {
        await _workVideoService.EnqueueCleanupForWorkAsync(request.Id, cancellationToken);
        return await _adminWorkService.DeleteAsync(request.Id, cancellationToken);
    }
}
