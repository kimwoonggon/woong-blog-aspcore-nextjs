using MediatR;
using WoongBlog.Application.Modules.Composition.Abstractions;

namespace WoongBlog.Application.Modules.Composition.GetHome;

public class GetHomeQueryHandler : IRequestHandler<GetHomeQuery, HomeDto?>
{
    private readonly IHomeQueryStore _homeQueryStore;

    public GetHomeQueryHandler(IHomeQueryStore homeQueryStore)
    {
        _homeQueryStore = homeQueryStore;
    }

    public async Task<HomeDto?> Handle(GetHomeQuery request, CancellationToken cancellationToken)
    {
        var homePage = await _homeQueryStore.GetHomePageAsync(cancellationToken);
        var siteSettings = await _homeQueryStore.GetSiteSettingsSummaryAsync(cancellationToken);

        if (homePage is null || siteSettings is null)
        {
            return null;
        }

        var featuredWorks = await _homeQueryStore.GetFeaturedWorksAsync(cancellationToken);
        var recentPosts = await _homeQueryStore.GetRecentPostsAsync(cancellationToken);

        return new HomeDto(homePage, siteSettings, featuredWorks, recentPosts);
    }
}
