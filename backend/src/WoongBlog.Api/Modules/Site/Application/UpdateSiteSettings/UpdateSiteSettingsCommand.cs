using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Site.Application.UpdateSiteSettings;

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
