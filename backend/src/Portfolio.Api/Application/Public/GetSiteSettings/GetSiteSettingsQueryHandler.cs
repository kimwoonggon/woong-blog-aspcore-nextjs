using MediatR;
using Portfolio.Api.Application.Public.Abstractions;

namespace Portfolio.Api.Application.Public.GetSiteSettings;

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
