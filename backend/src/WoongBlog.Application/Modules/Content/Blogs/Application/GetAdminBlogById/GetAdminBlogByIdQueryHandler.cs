using MediatR;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogById;

public sealed class GetAdminBlogByIdQueryHandler : IRequestHandler<GetAdminBlogByIdQuery, AdminBlogDetailDto?>
{
    private readonly IBlogQueryStore _blogQueryStore;

    public GetAdminBlogByIdQueryHandler(IBlogQueryStore blogQueryStore)
    {
        _blogQueryStore = blogQueryStore;
    }

    public async Task<AdminBlogDetailDto?> Handle(GetAdminBlogByIdQuery request, CancellationToken cancellationToken)
    {
        return await _blogQueryStore.GetAdminDetailAsync(request.Id, cancellationToken);
    }
}
