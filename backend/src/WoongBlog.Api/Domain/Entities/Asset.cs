namespace WoongBlog.Api.Domain.Entities;

public class Asset
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Bucket { get; private set; } = "media";
    public string Path { get; private set; } = string.Empty;
    public string PublicUrl { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long? Size { get; private set; }
    public string Kind { get; private set; } = "other";
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static Asset Create(string bucket, string path, string publicUrl, string mimeType, string kind, long? size, Guid? createdBy, DateTimeOffset createdAt, Guid? id = null)
    {
        return new Asset
        {
            Id = id ?? Guid.NewGuid(),
            Bucket = bucket,
            Path = path,
            PublicUrl = publicUrl,
            MimeType = mimeType,
            Kind = kind,
            Size = size,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };
    }
}
