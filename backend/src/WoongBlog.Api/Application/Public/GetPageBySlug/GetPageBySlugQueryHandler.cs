using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetPageBySlug;

public class GetPageBySlugQueryHandler : IRequestHandler<GetPageBySlugQuery, PageDto?>
{
    private readonly IPublicPageQueries _publicPageQueries;

    public GetPageBySlugQueryHandler(IPublicPageQueries publicPageQueries)
    {
        _publicPageQueries = publicPageQueries;
    }

    public async Task<PageDto?> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
    {
        return await _publicPageQueries.GetPageBySlugAsync(request.Slug, cancellationToken);
    }
}
