using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Works.GetWorks;

namespace WoongBlog.Application.Modules.Composition.GetHome;

public sealed record HomeDto(
    PageSummaryDto HomePage,
    SiteSettingsSummaryDto SiteSettings,
    IReadOnlyList<WorkCardDto> FeaturedWorks,
    IReadOnlyList<BlogCardDto> RecentPosts
);

public sealed record PageSummaryDto(string Title, string ContentJson);

public sealed record SiteSettingsSummaryDto(
    string OwnerName,
    string Tagline,
    string GitHubUrl,
    string LinkedInUrl,
    string ResumePublicUrl
);
