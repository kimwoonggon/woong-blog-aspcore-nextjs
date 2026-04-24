using MediatR;
using WoongBlog.Application.Modules.Content.Pages.Abstractions;

namespace WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;

public class GetPageBySlugQueryHandler : IRequestHandler<GetPageBySlugQuery, PageDto?>
{
    private readonly IPageQueryStore _pageQueryStore;

    public GetPageBySlugQueryHandler(IPageQueryStore pageQueryStore)
    {
        _pageQueryStore = pageQueryStore;
    }

    public async Task<PageDto?> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
    {
        return await _pageQueryStore.GetBySlugAsync(request.Slug, cancellationToken);
    }
}
