using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.DeleteWork;

public sealed class DeleteWorkCommandHandler : IRequestHandler<DeleteWorkCommand, AdminActionResult>
{
    private readonly IAdminWorkWriteStore _workWriteStore;

    public DeleteWorkCommandHandler(IAdminWorkWriteStore workWriteStore)
    {
        _workWriteStore = workWriteStore;
    }

    public async Task<AdminActionResult> Handle(DeleteWorkCommand request, CancellationToken cancellationToken)
    {
        var work = await _workWriteStore.FindByIdAsync(request.Id, cancellationToken);
        if (work is null)
        {
            return new AdminActionResult(false);
        }

        _workWriteStore.Remove(work);
        await _workWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
