namespace WoongBlog.Api.Domain.Entities;

public class SchemaPatch
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
}
