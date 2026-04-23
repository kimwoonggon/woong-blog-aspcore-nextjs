using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;

namespace WoongBlog.Api.Modules.Content.Blogs.Api.GetBlogs;

internal sealed class GetBlogsRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Query { get; init; }
    public string? SearchMode { get; init; }

    internal GetBlogsQuery ToQuery() => new(Page ?? 1, PageSize ?? 10, Query, SearchMode);
}
