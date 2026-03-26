using MediatR;
using Portfolio.Api.Application.Admin.Abstractions;

namespace Portfolio.Api.Application.Admin.GetAdminWorks;

public sealed class GetAdminWorksQueryHandler : IRequestHandler<GetAdminWorksQuery, IReadOnlyList<AdminWorkListItemDto>>
{
    private readonly IAdminWorkService _adminWorkService;

    public GetAdminWorksQueryHandler(IAdminWorkService adminWorkService)
    {
        _adminWorkService = adminWorkService;
    }

    public async Task<IReadOnlyList<AdminWorkListItemDto>> Handle(GetAdminWorksQuery request, CancellationToken cancellationToken)
    {
        return await _adminWorkService.GetAllAsync(cancellationToken);
    }
}
