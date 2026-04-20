using MediatR;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;

public class GetBlogsQueryHandler : IRequestHandler<GetBlogsQuery, PagedBlogsDto>
{
    private readonly IBlogQueryStore _blogQueryStore;

    public GetBlogsQueryHandler(IBlogQueryStore blogQueryStore)
    {
        _blogQueryStore = blogQueryStore;
    }

    public async Task<PagedBlogsDto> Handle(GetBlogsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Max(1, request.PageSize);
        var page = Math.Max(1, request.Page);
        var normalizedQuery = ContentSearchText.Normalize(request.Query);
        var searchMode = string.Equals(request.SearchMode.Trim(), "content", StringComparison.OrdinalIgnoreCase)
            ? ContentSearchMode.Content
            : ContentSearchMode.Title;

        return await _blogQueryStore.GetPublishedPageAsync(
            page,
            pageSize,
            string.IsNullOrEmpty(normalizedQuery) ? null : normalizedQuery,
            searchMode,
            cancellationToken);
    }
}
