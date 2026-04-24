using MediatR;
using WoongBlog.Application.Modules.Content.Pages.Abstractions;

namespace WoongBlog.Application.Modules.Content.Pages.GetAdminPages;

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
