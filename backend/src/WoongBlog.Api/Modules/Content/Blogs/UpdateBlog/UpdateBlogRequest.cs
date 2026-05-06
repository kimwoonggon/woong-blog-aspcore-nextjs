using WoongBlog.Application.Modules.Content.Blogs.UpdateBlog;

namespace WoongBlog.Api.Modules.Content.Blogs.UpdateBlog;

public sealed class UpdateBlogRequest
{
    private Guid? _coverAssetId;

    public string Title { get; init; } = string.Empty;
    public string? Excerpt { get; init; }
    public string[] Tags { get; init; } = [];
    public bool Published { get; init; }
    public string ContentJson { get; init; } = "{}";
    public Guid? CoverAssetId
    {
        get => _coverAssetId;
        init
        {
            _coverAssetId = value;
            HasCoverAssetId = true;
        }
    }

    internal bool HasCoverAssetId { get; private set; }

    internal UpdateBlogCommand ToCommand(Guid id) => new(
        id,
        Title,
        Excerpt,
        Tags,
        Published,
        ContentJson,
        CoverAssetId,
        HasCoverAssetId);
}
