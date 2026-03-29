using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminWorkById;

public sealed class GetAdminWorkByIdQueryHandler : IRequestHandler<GetAdminWorkByIdQuery, AdminWorkDetailDto?>
{
    private readonly IAdminWorkQueries _adminWorkQueries;

    public GetAdminWorkByIdQueryHandler(IAdminWorkQueries adminWorkQueries)
    {
        _adminWorkQueries = adminWorkQueries;
    }

    public async Task<AdminWorkDetailDto?> Handle(GetAdminWorkByIdQuery request, CancellationToken cancellationToken)
    {
        return await _adminWorkQueries.GetByIdAsync(request.Id, cancellationToken);
    }
}
