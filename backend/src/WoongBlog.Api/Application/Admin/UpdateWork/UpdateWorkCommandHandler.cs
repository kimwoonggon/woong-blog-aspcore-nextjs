using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.UpdateWork;

public sealed class UpdateWorkCommandHandler : IRequestHandler<UpdateWorkCommand, AdminMutationResult?>
{
    private readonly IAdminWorkService _adminWorkService;

    public UpdateWorkCommandHandler(IAdminWorkService adminWorkService)
    {
        _adminWorkService = adminWorkService;
    }

    public async Task<AdminMutationResult?> Handle(UpdateWorkCommand request, CancellationToken cancellationToken)
    {
        return await _adminWorkService.UpdateAsync(request, cancellationToken);
    }
}
