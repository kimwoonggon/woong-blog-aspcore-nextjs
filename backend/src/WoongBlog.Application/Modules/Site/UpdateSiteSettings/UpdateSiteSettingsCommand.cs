using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.Site.UpdateSiteSettings;

public sealed record UpdateSiteSettingsCommand(
    string? OwnerName,
    string? Tagline,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TwitterUrl,
    string? LinkedInUrl,
    string? GitHubUrl,
    Guid? ResumeAssetId,
    bool HasResumeAssetId
) : IRequest<AdminActionResult>;
