using WoongBlog.Api.Modules.Content.Blogs.Application.UpdateBlog;

namespace WoongBlog.Api.Modules.Content.Blogs.Api.UpdateBlog;

public sealed class UpdateBlogRequest
{
    public string Title { get; init; } = string.Empty;
    public string[] Tags { get; init; } = [];
    public bool Published { get; init; }
    public string ContentJson { get; init; } = "{}";

    internal UpdateBlogCommand ToCommand(Guid id) => new(id, Title, Tags, Published, ContentJson);
}
