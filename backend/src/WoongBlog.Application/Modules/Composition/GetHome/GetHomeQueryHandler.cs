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
        var shell = await _homeQueryStore.GetHomeShellAsync(cancellationToken);
        if (shell is null)
        {
            return null;
        }

        var featuredWorks = await _homeQueryStore.GetFeaturedWorksAsync(cancellationToken);
        var recentPosts = await _homeQueryStore.GetRecentPostsAsync(cancellationToken);

        return new HomeDto(shell.HomePage, shell.SiteSettings, featuredWorks, recentPosts);
    }
}
