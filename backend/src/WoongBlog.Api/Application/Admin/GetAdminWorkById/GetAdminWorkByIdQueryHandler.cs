using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminWorkById;

public sealed class GetAdminWorkByIdQueryHandler : IRequestHandler<GetAdminWorkByIdQuery, AdminWorkDetailDto?>
{
    private readonly IAdminWorkService _adminWorkService;

    public GetAdminWorkByIdQueryHandler(IAdminWorkService adminWorkService)
    {
        _adminWorkService = adminWorkService;
    }

    public async Task<AdminWorkDetailDto?> Handle(GetAdminWorkByIdQuery request, CancellationToken cancellationToken)
    {
        return await _adminWorkService.GetByIdAsync(request.Id, cancellationToken);
    }
}
