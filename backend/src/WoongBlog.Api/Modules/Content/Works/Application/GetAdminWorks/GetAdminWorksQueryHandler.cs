using MediatR;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetAdminWorks;

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
