using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Domain.Entities;

public class WorkVideoUploadSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkId { get; set; }
    public string StorageType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ExpectedMimeType { get; set; } = string.Empty;
    public long ExpectedSize { get; set; }
    public string Status { get; set; } = WorkVideoUploadSessionStatuses.Issued;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
