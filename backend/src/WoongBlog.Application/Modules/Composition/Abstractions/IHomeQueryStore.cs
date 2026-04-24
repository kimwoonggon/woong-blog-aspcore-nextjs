using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Works.GetWorks;

namespace WoongBlog.Application.Modules.Composition.Abstractions;

public interface IHomeQueryStore
{
    Task<PageSummaryDto?> GetHomePageAsync(CancellationToken cancellationToken);
    Task<SiteSettingsSummaryDto?> GetSiteSettingsSummaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkCardDto>> GetFeaturedWorksAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BlogCardDto>> GetRecentPostsAsync(CancellationToken cancellationToken);
}
