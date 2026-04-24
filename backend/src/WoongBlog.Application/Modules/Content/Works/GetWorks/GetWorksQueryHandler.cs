using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Works.Abstractions;

namespace WoongBlog.Application.Modules.Content.Works.GetWorks;

public class GetWorksQueryHandler : IRequestHandler<GetWorksQuery, PagedWorksDto>
{
    private readonly IWorkQueryStore _workQueryStore;

    public GetWorksQueryHandler(IWorkQueryStore workQueryStore)
    {
        _workQueryStore = workQueryStore;
    }

    public async Task<PagedWorksDto> Handle(GetWorksQuery request, CancellationToken cancellationToken)
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

        return await _workQueryStore.GetPublishedPageAsync(
            page,
            pageSize,
            string.IsNullOrEmpty(normalizedQuery) ? null : normalizedQuery,
            searchMode,
            cancellationToken);
    }
}
