using System.Text.Json;
using System.Text.Json.Serialization;

namespace Portfolio.Api.Application.Admin.GetAdminWorkById;

public sealed record AdminWorkContentDto(string Html);

public sealed record AdminWorkDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Excerpt,
    string Category,
    string? Period,
    string[] Tags,
    bool Published,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("all_properties")] JsonElement AllProperties,
    AdminWorkContentDto Content,
    [property: JsonPropertyName("thumbnail_asset_id")] Guid? ThumbnailAssetId,
    [property: JsonPropertyName("icon_asset_id")] Guid? IconAssetId,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailUrl,
    [property: JsonPropertyName("icon_url")] string IconUrl
);
