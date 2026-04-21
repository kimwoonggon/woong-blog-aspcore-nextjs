using MediatR;
using WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Pages.Application.GetAdminPages;

public sealed class GetAdminPagesQueryHandler : IRequestHandler<GetAdminPagesQuery, IReadOnlyList<AdminPageListItemDto>>
{
    private readonly IPageQueryStore _pageQueryStore;

    public GetAdminPagesQueryHandler(IPageQueryStore pageQueryStore)
    {
        _pageQueryStore = pageQueryStore;
    }

    public async Task<IReadOnlyList<AdminPageListItemDto>> Handle(GetAdminPagesQuery request, CancellationToken cancellationToken)
    {
        return await _pageQueryStore.GetAdminPagesAsync(request.Slugs, cancellationToken);
    }
}
