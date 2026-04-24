using System.Text.Json.Serialization;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;

public sealed record WorkDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    string ContentJson,
    string Category,
    string? Period,
    string[] Tags,
    string ThumbnailUrl,
    string IconUrl,
    DateTimeOffset? PublishedAt,
    [property: JsonPropertyName("socialShareMessage")] string? SocialShareMessage,
    [property: JsonPropertyName("videos_version")] int VideosVersion,
    IReadOnlyList<WorkVideoDto> Videos
);
