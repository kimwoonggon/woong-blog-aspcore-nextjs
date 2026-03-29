using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminWorks;

public sealed class GetAdminWorksQueryHandler : IRequestHandler<GetAdminWorksQuery, IReadOnlyList<AdminWorkListItemDto>>
{
    private readonly IAdminWorkQueries _adminWorkQueries;

    public GetAdminWorksQueryHandler(IAdminWorkQueries adminWorkQueries)
    {
        _adminWorkQueries = adminWorkQueries;
    }

    public async Task<IReadOnlyList<AdminWorkListItemDto>> Handle(GetAdminWorksQuery request, CancellationToken cancellationToken)
    {
        return await _adminWorkQueries.GetAllAsync(cancellationToken);
    }
}
