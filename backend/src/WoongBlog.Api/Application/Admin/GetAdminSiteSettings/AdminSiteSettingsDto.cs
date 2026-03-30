using System.Text.Json.Serialization;

namespace WoongBlog.Api.Application.Admin.GetAdminSiteSettings;

public sealed record AdminSiteSettingsDto(
    [property: JsonPropertyName("owner_name")] string OwnerName,
    [property: JsonPropertyName("tagline")] string Tagline,
    [property: JsonPropertyName("facebook_url")] string FacebookUrl,
    [property: JsonPropertyName("instagram_url")] string InstagramUrl,
    [property: JsonPropertyName("twitter_url")] string TwitterUrl,
    [property: JsonPropertyName("linkedin_url")] string LinkedInUrl,
    [property: JsonPropertyName("github_url")] string GitHubUrl,
    [property: JsonPropertyName("resume_asset_id")] Guid? ResumeAssetId,
    [property: JsonPropertyName("resume_asset")] AdminResumeAssetDto? ResumeAsset
);

public sealed record AdminResumeAssetDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("bucket")] string Bucket,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("public_url")] string PublicUrl,
    [property: JsonPropertyName("file_name")] string FileName
);
