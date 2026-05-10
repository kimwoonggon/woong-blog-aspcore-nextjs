using System.Text.Json.Serialization;
using WoongBlog.Application.Modules.Content.Common;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;

public sealed record WorkDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Excerpt,
    PublicContentBodyDto Content,
    string Category,
    string? Period,
    string[] Tags,
    string ThumbnailUrl,
    DateTimeOffset? PublishedAt,
    [property: JsonPropertyName("socialShareMessage")] string? SocialShareMessage,
    [property: JsonPropertyName("videos_version")] int VideosVersion,
    IReadOnlyList<PublicWorkVideoDto> Videos
);
