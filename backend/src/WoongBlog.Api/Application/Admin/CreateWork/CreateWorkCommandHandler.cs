using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.CreateWork;

public sealed class CreateWorkCommandHandler : IRequestHandler<CreateWorkCommand, AdminMutationResult>
{
    private readonly IAdminWorkService _adminWorkService;

    public CreateWorkCommandHandler(IAdminWorkService adminWorkService)
    {
        _adminWorkService = adminWorkService;
    }

    public async Task<AdminMutationResult> Handle(CreateWorkCommand request, CancellationToken cancellationToken)
    {
        return await _adminWorkService.CreateAsync(request, cancellationToken);
    }
}
