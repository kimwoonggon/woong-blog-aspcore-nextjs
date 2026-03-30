namespace WoongBlog.Api.Domain.Entities;

public class Work
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;
    public string ContentJson { get; private set; } = "{}";
    public Guid? ThumbnailAssetId { get; private set; }
    public Guid? IconAssetId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string? Period { get; private set; }
    public string AllPropertiesJson { get; private set; } = "{}";
    public string[] Tags { get; private set; } = Array.Empty<string>();
    public bool Published { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static Work Create(WorkUpsertValues values, string slug, string excerpt, DateTimeOffset now)
    {
        var work = new Work();
        work.Apply(values, slug, excerpt, now, isNew: true);
        return work;
    }

    public static Work Seed(
        WorkUpsertValues values,
        string slug,
        string excerpt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        Guid? id = null,
        DateTimeOffset? publishedAt = null)
    {
        return new Work
        {
            Id = id ?? Guid.NewGuid(),
            Slug = slug,
            Title = values.Title,
            Excerpt = excerpt,
            ContentJson = values.ContentJson,
            ThumbnailAssetId = values.ThumbnailAssetId,
            IconAssetId = values.IconAssetId,
            Category = values.Category,
            Period = values.Period,
            AllPropertiesJson = values.AllPropertiesJson,
            Tags = values.Tags,
            Published = values.Published,
            PublishedAt = publishedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void Update(WorkUpsertValues values, string slug, string excerpt, DateTimeOffset now)
    {
        Apply(values, slug, excerpt, now, isNew: false);
    }

    private void Apply(WorkUpsertValues values, string slug, string excerpt, DateTimeOffset now, bool isNew)
    {
        Title = values.Title;
        Slug = slug;
        Excerpt = excerpt;
        ThumbnailAssetId = values.ThumbnailAssetId;
        IconAssetId = values.IconAssetId;
        Category = values.Category;
        Period = values.Period;
        Tags = values.Tags;
        ContentJson = values.ContentJson;
        AllPropertiesJson = values.AllPropertiesJson;
        Published = values.Published;

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

public sealed record WorkUpsertValues(
    string Title,
    string Category,
    string Period,
    string[] Tags,
    bool Published,
    string ContentJson,
    string AllPropertiesJson,
    Guid? ThumbnailAssetId,
    Guid? IconAssetId);
