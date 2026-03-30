namespace WoongBlog.Api.Domain.Entities;

public class SiteSetting
{
    public bool Singleton { get; private set; } = true;
    public string OwnerName { get; private set; } = "Woonggon Kim";
    public string Tagline { get; private set; } = "Creative Technologist";
    public string FacebookUrl { get; private set; } = string.Empty;
    public string InstagramUrl { get; private set; } = string.Empty;
    public string TwitterUrl { get; private set; } = string.Empty;
    public string LinkedInUrl { get; private set; } = string.Empty;
    public string GitHubUrl { get; private set; } = string.Empty;
    public Guid? ResumeAssetId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static SiteSetting Create(
        string ownerName,
        string tagline,
        string facebookUrl,
        string instagramUrl,
        string twitterUrl,
        string linkedInUrl,
        string gitHubUrl,
        Guid? resumeAssetId,
        DateTimeOffset updatedAt)
    {
        return new SiteSetting
        {
            Singleton = true,
            OwnerName = ownerName,
            Tagline = tagline,
            FacebookUrl = facebookUrl,
            InstagramUrl = instagramUrl,
            TwitterUrl = twitterUrl,
            LinkedInUrl = linkedInUrl,
            GitHubUrl = gitHubUrl,
            ResumeAssetId = resumeAssetId,
            UpdatedAt = updatedAt
        };
    }

    public void ApplyUpdate(SiteSettingUpdateValues values, DateTimeOffset updatedAt)
    {
        if (values.OwnerName is not null) OwnerName = values.OwnerName;
        if (values.Tagline is not null) Tagline = values.Tagline;
        if (values.FacebookUrl is not null) FacebookUrl = values.FacebookUrl;
        if (values.InstagramUrl is not null) InstagramUrl = values.InstagramUrl;
        if (values.TwitterUrl is not null) TwitterUrl = values.TwitterUrl;
        if (values.LinkedInUrl is not null) LinkedInUrl = values.LinkedInUrl;
        if (values.GitHubUrl is not null) GitHubUrl = values.GitHubUrl;
        if (values.HasResumeAssetId) ResumeAssetId = values.ResumeAssetId;

        UpdatedAt = updatedAt;
    }
}

public sealed record SiteSettingUpdateValues(
    string? OwnerName,
    string? Tagline,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TwitterUrl,
    string? LinkedInUrl,
    string? GitHubUrl,
    Guid? ResumeAssetId,
    bool HasResumeAssetId);
