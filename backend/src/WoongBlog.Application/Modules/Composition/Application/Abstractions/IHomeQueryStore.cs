using WoongBlog.Api.Modules.Composition.Application.GetHome;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorks;

namespace WoongBlog.Api.Modules.Composition.Application.Abstractions;

public interface IHomeQueryStore
{
    Task<PageSummaryDto?> GetHomePageAsync(CancellationToken cancellationToken);
    Task<SiteSettingsSummaryDto?> GetSiteSettingsSummaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkCardDto>> GetFeaturedWorksAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BlogCardDto>> GetRecentPostsAsync(CancellationToken cancellationToken);
}
