using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Site.Application.Abstractions;

namespace WoongBlog.Api.Modules.Site.Application.UpdateSiteSettings;

public sealed class UpdateSiteSettingsCommandHandler : IRequestHandler<UpdateSiteSettingsCommand, AdminActionResult>
{
    private readonly ISiteSettingsCommandStore _siteSettingsCommandStore;

    public UpdateSiteSettingsCommandHandler(ISiteSettingsCommandStore siteSettingsCommandStore)
    {
        _siteSettingsCommandStore = siteSettingsCommandStore;
    }

    public async Task<AdminActionResult> Handle(UpdateSiteSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _siteSettingsCommandStore.GetForUpdateAsync(cancellationToken);
        if (settings is null)
        {
            return new AdminActionResult(false);
        }

        if (request.OwnerName is not null) settings.OwnerName = request.OwnerName;
        if (request.Tagline is not null) settings.Tagline = request.Tagline;
        if (request.FacebookUrl is not null) settings.FacebookUrl = request.FacebookUrl;
        if (request.InstagramUrl is not null) settings.InstagramUrl = request.InstagramUrl;
        if (request.TwitterUrl is not null) settings.TwitterUrl = request.TwitterUrl;
        if (request.LinkedInUrl is not null) settings.LinkedInUrl = request.LinkedInUrl;
        if (request.GitHubUrl is not null) settings.GitHubUrl = request.GitHubUrl;
        if (request.HasResumeAssetId) settings.ResumeAssetId = request.ResumeAssetId;

        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await _siteSettingsCommandStore.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
