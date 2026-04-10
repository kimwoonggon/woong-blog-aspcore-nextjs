using System.Text.Json.Serialization;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetWorkBySlug;

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
    [property: JsonPropertyName("videos_version")] int VideosVersion,
    IReadOnlyList<WorkVideoDto> Videos
);
