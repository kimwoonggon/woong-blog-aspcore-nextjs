using MediatR;
using WoongBlog.Api.Modules.Site.Application.Abstractions;

namespace WoongBlog.Api.Modules.Site.Application.GetSiteSettings;

public class GetSiteSettingsQueryHandler : IRequestHandler<GetSiteSettingsQuery, SiteSettingsDto?>
{
    private readonly ISiteSettingsQueryStore _siteSettingsQueryStore;

    public GetSiteSettingsQueryHandler(ISiteSettingsQueryStore siteSettingsQueryStore)
    {
        _siteSettingsQueryStore = siteSettingsQueryStore;
    }

    public async Task<SiteSettingsDto?> Handle(GetSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _siteSettingsQueryStore.GetPublicSettingsAsync(cancellationToken);
    }
}
