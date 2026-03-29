using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetSiteSettings;

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
