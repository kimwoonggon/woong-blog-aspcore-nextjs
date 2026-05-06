using System.Text.Json.Serialization;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Common;
using WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;
using WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;
using WoongBlog.Application.Modules.Content.Works.GetWorks;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;
using WoongBlog.Application.Modules.Site.GetResume;
using WoongBlog.Application.Modules.Site.GetSiteSettings;
using WoongBlog.Infrastructure.LoadTesting;

namespace WoongBlog.Api.Common.Json;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(HomeDto))]
[JsonSerializable(typeof(HomeShellDto))]
[JsonSerializable(typeof(PageSummaryDto))]
[JsonSerializable(typeof(SiteSettingsSummaryDto))]
[JsonSerializable(typeof(PagedBlogsDto))]
[JsonSerializable(typeof(BlogCardDto))]
[JsonSerializable(typeof(BlogDetailDto))]
[JsonSerializable(typeof(PagedWorksDto))]
[JsonSerializable(typeof(WorkCardDto))]
[JsonSerializable(typeof(WorkDetailDto))]
[JsonSerializable(typeof(PublicContentBodyDto))]
[JsonSerializable(typeof(WorkVideoDto))]
[JsonSerializable(typeof(WorkPublicVideoSnapshot))]
[JsonSerializable(typeof(PageDto))]
[JsonSerializable(typeof(SiteSettingsDto))]
[JsonSerializable(typeof(ResumeDto))]
[JsonSerializable(typeof(RealLoadTestStartResponse))]
[JsonSerializable(typeof(RealLoadTestStatusResponse))]
[JsonSerializable(typeof(RealLoadTestMetricsResponse))]
[JsonSerializable(typeof(RealLoadTestMetricPoint))]
[JsonSerializable(typeof(RealLoadTestStopResponse))]
[JsonSerializable(typeof(RealLoadTestTargetSpec))]
[JsonSerializable(typeof(RealLoadTestLatencyBreakdown))]
[JsonSerializable(typeof(RealLoadTestTargetMetrics))]
[JsonSerializable(typeof(LoadTestDiagnosticsSnapshot))]
[JsonSerializable(typeof(LoadTestProcessDiagnostics))]
[JsonSerializable(typeof(LoadTestGcDiagnostics))]
[JsonSerializable(typeof(LoadTestThreadPoolDiagnostics))]
[JsonSerializable(typeof(LoadTestDatabaseDiagnostics))]
[JsonSerializable(typeof(LoadTestDatabaseLatencyView))]
[JsonSerializable(typeof(LoadTestDatabasePoolDiagnostics))]
[JsonSerializable(typeof(LoadTestSlowQueryView))]
internal sealed partial class WoongBlogApiJsonSerializerContext : JsonSerializerContext;
