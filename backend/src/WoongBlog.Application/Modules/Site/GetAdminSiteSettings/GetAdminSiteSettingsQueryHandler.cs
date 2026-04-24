using MediatR;
using WoongBlog.Application.Modules.Site.Abstractions;

namespace WoongBlog.Application.Modules.Site.GetAdminSiteSettings;

public sealed class GetAdminSiteSettingsQueryHandler : IRequestHandler<GetAdminSiteSettingsQuery, AdminSiteSettingsDto?>
{
    private readonly ISiteSettingsQueryStore _siteSettingsQueryStore;

    public GetAdminSiteSettingsQueryHandler(ISiteSettingsQueryStore siteSettingsQueryStore)
    {
        _siteSettingsQueryStore = siteSettingsQueryStore;
    }

    public async Task<AdminSiteSettingsDto?> Handle(GetAdminSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _siteSettingsQueryStore.GetAdminSettingsAsync(cancellationToken);
    }
}
