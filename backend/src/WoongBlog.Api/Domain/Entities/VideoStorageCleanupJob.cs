namespace WoongBlog.Api.Domain.Entities;

public class VideoStorageCleanupJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? WorkId { get; set; }
    public Guid? WorkVideoId { get; set; }
    public string StorageType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string Status { get; set; } = VideoStorageCleanupJobStatuses.Pending;
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
