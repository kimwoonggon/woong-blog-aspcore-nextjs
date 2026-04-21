using MediatR;
using WoongBlog.Api.Modules.Site.Application.Abstractions;

namespace WoongBlog.Api.Modules.Site.Application.GetAdminSiteSettings;

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
