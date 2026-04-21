using MediatR;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogBySlug;

public class GetBlogBySlugQueryHandler : IRequestHandler<GetBlogBySlugQuery, BlogDetailDto?>
{
    private readonly IBlogQueryStore _blogQueryStore;

    public GetBlogBySlugQueryHandler(IBlogQueryStore blogQueryStore)
    {
        _blogQueryStore = blogQueryStore;
    }

    public async Task<BlogDetailDto?> Handle(GetBlogBySlugQuery request, CancellationToken cancellationToken)
    {
        return await _blogQueryStore.GetPublishedDetailBySlugAsync(request.Slug, cancellationToken);
    }
}
