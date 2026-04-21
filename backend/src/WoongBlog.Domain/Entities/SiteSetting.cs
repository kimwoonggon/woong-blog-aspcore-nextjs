namespace WoongBlog.Api.Domain.Entities;

public class SiteSetting
{
    public bool Singleton { get; set; } = true;
    public string OwnerName { get; set; } = "Woonggon Kim";
    public string Tagline { get; set; } = "Creative Technologist";
    public string FacebookUrl { get; set; } = string.Empty;
    public string InstagramUrl { get; set; } = string.Empty;
    public string TwitterUrl { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string GitHubUrl { get; set; } = string.Empty;
    public Guid? ResumeAssetId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
