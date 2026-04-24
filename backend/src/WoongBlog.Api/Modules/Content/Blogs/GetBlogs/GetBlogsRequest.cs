using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;

namespace WoongBlog.Api.Modules.Content.Blogs.GetBlogs;

internal sealed class GetBlogsRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Query { get; init; }
    public string? SearchMode { get; init; }

    internal GetBlogsQuery ToQuery() => new(Page ?? 1, PageSize ?? 10, Query, SearchMode);
}
