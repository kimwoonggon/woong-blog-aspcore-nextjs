using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetPageBySlug;

public class GetPageBySlugQueryHandler : IRequestHandler<GetPageBySlugQuery, PageDto?>
{
    private readonly IPublicPageService _publicPageService;

    public GetPageBySlugQueryHandler(IPublicPageService publicPageService)
    {
        _publicPageService = publicPageService;
    }

    public async Task<PageDto?> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
    {
        return await _publicPageService.GetPageBySlugAsync(request.Slug, cancellationToken);
    }
}
