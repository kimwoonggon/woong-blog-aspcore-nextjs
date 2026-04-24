namespace WoongBlog.Application.Modules.Site.GetSiteSettings;

public sealed record SiteSettingsDto(
    string OwnerName,
    string Tagline,
    string FacebookUrl,
    string InstagramUrl,
    string TwitterUrl,
    string LinkedInUrl,
    string GitHubUrl
);
