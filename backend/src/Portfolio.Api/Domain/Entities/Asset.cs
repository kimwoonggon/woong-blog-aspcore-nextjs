namespace Portfolio.Api.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Bucket { get; set; } = "media";
    public string Path { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long? Size { get; set; }
    public string Kind { get; set; } = "other";
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
