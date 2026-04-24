using MediatR;
using WoongBlog.Application.Modules.Site.Abstractions;

namespace WoongBlog.Application.Modules.Site.GetSiteSettings;

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
