using WoongBlog.Api.Modules.Content.Works.Application.UpdateWork;

namespace WoongBlog.Api.Modules.Content.Works.Api.UpdateWork;

public sealed class UpdateWorkRequest
{
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Period { get; init; } = string.Empty;
    public string[] Tags { get; init; } = [];
    public bool Published { get; init; }
    public string ContentJson { get; init; } = "{}";
    public string AllPropertiesJson { get; init; } = "{}";
    public Guid? ThumbnailAssetId { get; init; }
    public Guid? IconAssetId { get; init; }

    internal UpdateWorkCommand ToCommand(Guid id) => new(
        id,
        Title,
        Category,
        Period,
        Tags,
        Published,
        ContentJson,
        AllPropertiesJson,
        ThumbnailAssetId,
        IconAssetId);
}
