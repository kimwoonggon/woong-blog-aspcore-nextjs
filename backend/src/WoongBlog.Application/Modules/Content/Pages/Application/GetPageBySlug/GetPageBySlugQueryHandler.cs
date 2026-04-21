using MediatR;
using WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Pages.Application.GetPageBySlug;

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
