using WoongBlog.Application.Modules.Content.Blogs.CreateBlog;

namespace WoongBlog.Api.Modules.Content.Blogs.CreateBlog;

public sealed class CreateBlogRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Excerpt { get; init; }
    public string[] Tags { get; init; } = [];
    public bool Published { get; init; }
    public string ContentJson { get; init; } = "{}";

    internal CreateBlogCommand ToCommand() => new(Title, Excerpt, Tags, Published, ContentJson);
}
