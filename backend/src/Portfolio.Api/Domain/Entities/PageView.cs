namespace Portfolio.Api.Domain.Entities;

public class PageView
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Path { get; set; } = string.Empty;
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public DateOnly ViewedOn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string? UserAgent { get; set; }
    public string? Referrer { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
