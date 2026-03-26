using MediatR;
using Portfolio.Api.Application.Admin.Abstractions;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.DeleteWork;

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
