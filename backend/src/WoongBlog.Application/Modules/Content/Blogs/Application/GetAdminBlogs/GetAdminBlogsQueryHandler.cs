using MediatR;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogs;

public sealed class GetAdminBlogsQueryHandler : IRequestHandler<GetAdminBlogsQuery, IReadOnlyList<AdminBlogListItemDto>>
{
    private readonly IBlogQueryStore _blogQueryStore;

    public GetAdminBlogsQueryHandler(IBlogQueryStore blogQueryStore)
    {
        _blogQueryStore = blogQueryStore;
    }

    public async Task<IReadOnlyList<AdminBlogListItemDto>> Handle(GetAdminBlogsQuery request, CancellationToken cancellationToken)
    {
        return await _blogQueryStore.GetAdminListAsync(cancellationToken);
    }
}
