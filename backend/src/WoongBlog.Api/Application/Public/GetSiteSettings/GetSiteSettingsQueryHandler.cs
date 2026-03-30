using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetSiteSettings;

public class GetSiteSettingsQueryHandler : IRequestHandler<GetSiteSettingsQuery, SiteSettingsDto?>
{
    private readonly IPublicSiteQueries _publicSiteQueries;

    public GetSiteSettingsQueryHandler(IPublicSiteQueries publicSiteQueries)
    {
        _publicSiteQueries = publicSiteQueries;
    }

    public async Task<SiteSettingsDto?> Handle(GetSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _publicSiteQueries.GetSiteSettingsAsync(cancellationToken);
    }
}
