using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Works.Application.DeleteWork;

public sealed class DeleteWorkCommandHandler : IRequestHandler<DeleteWorkCommand, AdminActionResult>
{
    private readonly IAdminWorkService _adminWorkService;

    public DeleteWorkCommandHandler(IAdminWorkService adminWorkService)
    {
        _adminWorkService = adminWorkService;
    }

    public async Task<AdminActionResult> Handle(DeleteWorkCommand request, CancellationToken cancellationToken)
    {
        return await _adminWorkService.DeleteAsync(request.Id, cancellationToken);
    }
}
