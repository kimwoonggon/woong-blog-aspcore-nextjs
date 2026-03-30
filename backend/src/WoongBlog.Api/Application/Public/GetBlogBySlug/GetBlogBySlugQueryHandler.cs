using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetBlogBySlug;

public class GetBlogBySlugQueryHandler : IRequestHandler<GetBlogBySlugQuery, BlogDetailDto?>
{
    private readonly IPublicBlogQueries _publicBlogQueries;

    public GetBlogBySlugQueryHandler(IPublicBlogQueries publicBlogQueries)
    {
        _publicBlogQueries = publicBlogQueries;
    }

    public async Task<BlogDetailDto?> Handle(GetBlogBySlugQuery request, CancellationToken cancellationToken)
    {
        return await _publicBlogQueries.GetBlogBySlugAsync(request.Slug, cancellationToken);
    }
}
