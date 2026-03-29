namespace WoongBlog.Api.Controllers.Models;

public class SaveWorkRequest
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public bool Published { get; set; }
    public string ContentJson { get; set; } = "{}";
    public string AllPropertiesJson { get; set; } = "{}";
    public Guid? ThumbnailAssetId { get; set; }
    public Guid? IconAssetId { get; set; }
}
