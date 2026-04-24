using WoongBlog.Application.Modules.Content.Works.GetWorks;

namespace WoongBlog.Api.Modules.Content.Works.GetWorks;

internal sealed class GetWorksRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Query { get; init; }
    public string? SearchMode { get; init; }

    internal GetWorksQuery ToQuery() => new(Page ?? 1, PageSize ?? 6, Query, SearchMode);
}
