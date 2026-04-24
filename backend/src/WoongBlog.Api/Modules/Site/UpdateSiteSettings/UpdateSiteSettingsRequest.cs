using System.Text.Json.Serialization;
using WoongBlog.Application.Modules.Site.UpdateSiteSettings;

namespace WoongBlog.Api.Modules.Site.UpdateSiteSettings;

public sealed class UpdateSiteSettingsRequest
{
    public string? OwnerName { get; init; }
    public string? Tagline { get; init; }
    public string? FacebookUrl { get; init; }
    public string? InstagramUrl { get; init; }
    public string? TwitterUrl { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? GitHubUrl { get; init; }

    private Guid? _resumeAssetId;

    public Guid? ResumeAssetId
    {
        get => _resumeAssetId;
        init
        {
            _resumeAssetId = value;
            HasResumeAssetId = true;
        }
    }

    [JsonIgnore]
    public bool HasResumeAssetId { get; private init; }

    internal UpdateSiteSettingsCommand ToCommand() => new(
        OwnerName,
        Tagline,
        FacebookUrl,
        InstagramUrl,
        TwitterUrl,
        LinkedInUrl,
        GitHubUrl,
        ResumeAssetId,
        HasResumeAssetId);
}
