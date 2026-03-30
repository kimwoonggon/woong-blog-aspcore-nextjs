namespace WoongBlog.Api.Domain.Entities;

public class Blog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;
    public string ContentJson { get; private set; } = "{}";
    public Guid? CoverAssetId { get; private set; }
    public string[] Tags { get; private set; } = Array.Empty<string>();
    public bool Published { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static Blog Create(BlogUpsertValues values, string slug, string excerpt, DateTimeOffset now)
    {
        var blog = new Blog();
        blog.Apply(values, slug, excerpt, now, isNew: true);
        return blog;
    }

    public static Blog Seed(
        BlogUpsertValues values,
        string slug,
        string excerpt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        Guid? id = null,
        DateTimeOffset? publishedAt = null)
    {
        return new Blog
        {
            Id = id ?? Guid.NewGuid(),
            Slug = slug,
            Title = values.Title,
            Excerpt = excerpt,
            ContentJson = values.ContentJson,
            CoverAssetId = values.CoverAssetId,
            Tags = values.Tags,
            Published = values.Published,
            PublishedAt = publishedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void Update(BlogUpsertValues values, string slug, string excerpt, DateTimeOffset now)
    {
        Apply(values, slug, excerpt, now, isNew: false);
    }

    public void ApplyFixedHtml(string contentJson, string excerpt, DateTimeOffset now)
    {
        ContentJson = contentJson;
        Excerpt = excerpt;
        UpdatedAt = now;
    }

    private void Apply(BlogUpsertValues values, string slug, string excerpt, DateTimeOffset now, bool isNew)
    {
        Title = values.Title;
        Slug = slug;
        Excerpt = excerpt;
        Tags = values.Tags;
        Published = values.Published;
        ContentJson = values.ContentJson;
        CoverAssetId = values.CoverAssetId;

        if (isNew)
        {
            Id = Guid.NewGuid();
            CreatedAt = now;
        }

        if (values.Published && PublishedAt is null)
        {
            PublishedAt = now;
        }

        UpdatedAt = now;
    }
}

public sealed record BlogUpsertValues(
    string Title,
    string[] Tags,
    bool Published,
    string ContentJson,
    Guid? CoverAssetId = null);
