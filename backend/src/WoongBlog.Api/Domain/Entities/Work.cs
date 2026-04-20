namespace WoongBlog.Api.Domain.Entities;

public class Work
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string ContentJson { get; set; } = "{}";
    public string SearchTitle { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public Guid? ThumbnailAssetId { get; set; }
    public Guid? IconAssetId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Period { get; set; }
    public string AllPropertiesJson { get; set; } = "{}";
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int VideosVersion { get; set; }
    public bool Published { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
