using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Works.Abstractions;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Application.Modules.Content.Works.DeleteWork;

public sealed class DeleteWorkCommandHandler : IRequestHandler<DeleteWorkCommand, AdminActionResult>
{
    private readonly IWorkCommandStore _workCommandStore;
    private readonly IWorkVideoCleanupService _workVideoCleanupService;

    public DeleteWorkCommandHandler(IWorkCommandStore workCommandStore, IWorkVideoCleanupService workVideoCleanupService)
    {
        _workCommandStore = workCommandStore;
        _workVideoCleanupService = workVideoCleanupService;
    }

    public async Task<AdminActionResult> Handle(DeleteWorkCommand request, CancellationToken cancellationToken)
    {
        var work = await _workCommandStore.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (work is null)
        {
            return new AdminActionResult(false);
        }

        await _workVideoCleanupService.EnqueueCleanupForWorkAsync(request.Id, cancellationToken);
        var workVideos = await _workCommandStore.GetVideosForWorkAsync(request.Id, cancellationToken);
        var uploadSessions = await _workCommandStore.GetUploadSessionsForWorkAsync(request.Id, cancellationToken);

        _workCommandStore.RemoveVideos(workVideos);
        _workCommandStore.RemoveUploadSessions(uploadSessions);
        _workCommandStore.Remove(work);
        await _workCommandStore.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
