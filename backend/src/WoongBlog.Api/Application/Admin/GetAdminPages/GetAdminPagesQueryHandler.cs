using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminPages;

public sealed class GetAdminPagesQueryHandler : IRequestHandler<GetAdminPagesQuery, IReadOnlyList<AdminPageListItemDto>>
{
    private readonly IAdminPageService _adminPageService;

    public GetAdminPagesQueryHandler(IAdminPageService adminPageService)
    {
        _adminPageService = adminPageService;
    }

    public async Task<IReadOnlyList<AdminPageListItemDto>> Handle(GetAdminPagesQuery request, CancellationToken cancellationToken)
    {
        return await _adminPageService.GetPagesAsync(request.Slugs, cancellationToken);
    }
}
