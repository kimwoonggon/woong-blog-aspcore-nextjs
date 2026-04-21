namespace WoongBlog.Api.Domain.Entities;

public class WorkVideo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
