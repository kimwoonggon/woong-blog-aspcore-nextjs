using MediatR;
using WoongBlog.Api.Modules.Site.Application.Abstractions;

namespace WoongBlog.Api.Modules.Site.Application.GetSiteSettings;

public class GetSiteSettingsQueryHandler : IRequestHandler<GetSiteSettingsQuery, SiteSettingsDto?>
{
    private readonly IPublicSiteService _publicSiteService;

    public GetSiteSettingsQueryHandler(IPublicSiteService publicSiteService)
    {
        _publicSiteService = publicSiteService;
    }

    public async Task<SiteSettingsDto?> Handle(GetSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _publicSiteService.GetSiteSettingsAsync(cancellationToken);
    }
}
