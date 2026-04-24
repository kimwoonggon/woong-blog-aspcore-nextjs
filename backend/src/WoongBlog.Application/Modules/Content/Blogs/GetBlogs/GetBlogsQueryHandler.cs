using MediatR;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.Content.Blogs.GetBlogs;

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
        var searchMode = request.SearchMode?.Trim().ToLowerInvariant() switch
        {
            "title" => ContentSearchMode.Title,
            "content" => ContentSearchMode.Content,
            _ => ContentSearchMode.Unified
        };

        return await _blogQueryStore.GetPublishedPageAsync(
            page,
            pageSize,
            string.IsNullOrEmpty(normalizedQuery) ? null : normalizedQuery,
            searchMode,
            cancellationToken);
    }
}
