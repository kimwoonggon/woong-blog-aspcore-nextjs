using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Works.Application.CreateWork;

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
