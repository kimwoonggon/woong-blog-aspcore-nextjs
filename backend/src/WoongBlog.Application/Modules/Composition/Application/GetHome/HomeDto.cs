using WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogs;
using WoongBlog.Api.Modules.Content.Works.Application.GetWorks;

namespace WoongBlog.Api.Modules.Composition.Application.GetHome;

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
