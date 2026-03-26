using MediatR;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.UpdateSiteSettings;

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
