using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.UpdateSiteSettings;

public sealed class UpdateSiteSettingsCommandHandler : IRequestHandler<UpdateSiteSettingsCommand, AdminActionResult>
{
    private readonly IAdminSiteSettingsWriteStore _siteSettingsWriteStore;

    public UpdateSiteSettingsCommandHandler(IAdminSiteSettingsWriteStore siteSettingsWriteStore)
    {
        _siteSettingsWriteStore = siteSettingsWriteStore;
    }

    public async Task<AdminActionResult> Handle(UpdateSiteSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _siteSettingsWriteStore.GetSingletonAsync(cancellationToken);
        if (settings is null)
        {
            return new AdminActionResult(false);
        }

        settings.ApplyUpdate(new SiteSettingUpdateValues(
            request.OwnerName,
            request.Tagline,
            request.FacebookUrl,
            request.InstagramUrl,
            request.TwitterUrl,
            request.LinkedInUrl,
            request.GitHubUrl,
            request.ResumeAssetId,
            request.HasResumeAssetId), DateTimeOffset.UtcNow);
        await _siteSettingsWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
