using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminPages;

public sealed class GetAdminPagesQueryHandler : IRequestHandler<GetAdminPagesQuery, IReadOnlyList<AdminPageListItemDto>>
{
    private readonly IAdminPageQueries _adminPageQueries;

    public GetAdminPagesQueryHandler(IAdminPageQueries adminPageQueries)
    {
        _adminPageQueries = adminPageQueries;
    }

    public async Task<IReadOnlyList<AdminPageListItemDto>> Handle(GetAdminPagesQuery request, CancellationToken cancellationToken)
    {
        return await _adminPageQueries.GetPagesAsync(request.Slugs, cancellationToken);
    }
}
