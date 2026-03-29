using System.Text.Json.Serialization;

namespace WoongBlog.Api.Controllers.Models;

public class UpdateSiteSettingsRequest
{
    public string? OwnerName { get; set; }
    public string? Tagline { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }

    private Guid? _resumeAssetId;

    public Guid? ResumeAssetId
    {
        get => _resumeAssetId;
        set
        {
            _resumeAssetId = value;
            HasResumeAssetId = true;
        }
    }

    [JsonIgnore]
    public bool HasResumeAssetId { get; private set; }
}
