using MediatR;
using Portfolio.Api.Application.Public.Abstractions;

namespace Portfolio.Api.Application.Public.GetBlogBySlug;

public class GetBlogBySlugQueryHandler : IRequestHandler<GetBlogBySlugQuery, BlogDetailDto?>
{
    private readonly IPublicBlogService _publicBlogService;

    public GetBlogBySlugQueryHandler(IPublicBlogService publicBlogService)
    {
        _publicBlogService = publicBlogService;
    }

    public async Task<BlogDetailDto?> Handle(GetBlogBySlugQuery request, CancellationToken cancellationToken)
    {
        return await _publicBlogService.GetBlogBySlugAsync(request.Slug, cancellationToken);
    }
}
