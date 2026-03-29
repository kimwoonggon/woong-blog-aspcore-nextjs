using MediatR;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.UpdateSiteSettings;

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
