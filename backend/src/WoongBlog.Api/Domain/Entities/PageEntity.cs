namespace WoongBlog.Api.Domain.Entities;

public class PageEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string ContentJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static PageEntity Create(string slug, string title, string contentJson, Guid? id = null, DateTimeOffset? updatedAt = null)
    {
        return new PageEntity
        {
            Id = id ?? Guid.NewGuid(),
            Slug = slug,
            Title = title,
            ContentJson = contentJson,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow
        };
    }

    public void UpdateContent(string title, string contentJson, DateTimeOffset updatedAt)
    {
        Title = title;
        ContentJson = contentJson;
        UpdatedAt = updatedAt;
    }
}
