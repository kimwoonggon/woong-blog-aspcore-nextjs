namespace WoongBlog.Api.Application.Public.GetHome;

public sealed record HomeDto(
    PageSummaryDto HomePage,
    SiteSettingsSummaryDto SiteSettings,
    IReadOnlyList<WorkCardDto> FeaturedWorks,
    IReadOnlyList<BlogCardDto> RecentPosts
);

public sealed record PageSummaryDto(string Title, string ContentJson);

public sealed record SiteSettingsSummaryDto(
    string OwnerName,
    string Tagline,
    string GitHubUrl,
    string LinkedInUrl,
    string ResumePublicUrl
);

public sealed record WorkCardDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string Category,
    string? Period,
    string[] Tags,
    string? ThumbnailUrl,
    string? IconUrl,
    DateTimeOffset? PublishedAt
);

public sealed record BlogCardDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string[] Tags,
    string? CoverUrl,
    DateTimeOffset? PublishedAt
);
