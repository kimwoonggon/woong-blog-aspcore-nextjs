namespace WoongBlog.Api.Domain.Entities;

public class AuthAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ProfileId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Provider { get; set; } = "google";
    public string ProviderSubject { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
