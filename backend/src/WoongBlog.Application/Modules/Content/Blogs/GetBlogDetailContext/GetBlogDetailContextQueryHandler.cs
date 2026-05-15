using MediatR;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;

namespace WoongBlog.Application.Modules.Content.Blogs.GetBlogDetailContext;

public sealed class GetBlogDetailContextQueryHandler(IBlogQueryStore blogQueryStore)
    : IRequestHandler<GetBlogDetailContextQuery, BlogDetailContextDto?>
{
    public async Task<BlogDetailContextDto?> Handle(
        GetBlogDetailContextQuery request,
        CancellationToken cancellationToken)
    {
        return await blogQueryStore.GetPublishedDetailContextBySlugAsync(
            request.Slug,
            request.Limit,
            cancellationToken);
    }
}
