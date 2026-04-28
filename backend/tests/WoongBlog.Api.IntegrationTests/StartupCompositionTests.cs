using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Auth;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Proxy;
using WoongBlog.Infrastructure.Security;
using WoongBlog.Infrastructure.Storage;
using WoongBlog.Infrastructure.Ai;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;
using WoongBlog.Infrastructure.Modules.Identity.Services;
using WoongBlog.Application.Modules.AI;
using WoongBlog.Application.Modules.AI.Abstractions;
using WoongBlog.Application.Modules.AI.BatchJobs;
using WoongBlog.Application.Modules.AI.BlogFix;
using WoongBlog.Application.Modules.AI.RuntimeConfig;
using WoongBlog.Application.Modules.AI.WorkEnrich;
using WoongBlog.Application.Modules.Composition.Abstractions;
using WoongBlog.Application.Modules.Composition.GetDashboardSummary;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Api.Modules.Content.Blogs.CreateBlog;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;
using WoongBlog.Application.Modules.Content.Blogs.CreateBlog;
using WoongBlog.Application.Modules.Content.Blogs.DeleteBlog;
using WoongBlog.Application.Modules.Content.Blogs.GetAdminBlogs;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Blogs.UpdateBlog;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Pages.Abstractions;
using WoongBlog.Application.Modules.Content.Pages.GetAdminPages;
using WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;
using WoongBlog.Application.Modules.Content.Pages.UpdatePage;
using WoongBlog.Application.Modules.Content.Works.Abstractions;
using WoongBlog.Application.Modules.Content.Works.CreateWork;
using WoongBlog.Application.Modules.Content.Works.DeleteWork;
using WoongBlog.Application.Modules.Content.Works.GetAdminWorks;
using WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;
using WoongBlog.Application.Modules.Content.Works.GetWorks;
using WoongBlog.Application.Modules.Content.Works.UpdateWork;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;
using WoongBlog.Application.Modules.Identity.Abstractions;
using WoongBlog.Application.Modules.Identity.GetAdminMembers;
using WoongBlog.Application.Modules.Media.Abstractions;
using WoongBlog.Application.Modules.Media.Commands.DeleteMediaAsset;
using WoongBlog.Application.Modules.Media.Commands.UploadMediaAsset;
using WoongBlog.Application.Modules.Media.Results;
using WoongBlog.Application.Modules.Site.Abstractions;
using WoongBlog.Application.Modules.Site.GetAdminSiteSettings;
using WoongBlog.Application.Modules.Site.GetResume;
using WoongBlog.Application.Modules.Site.GetSiteSettings;
using WoongBlog.Application.Modules.Site.UpdateSiteSettings;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Integration)]
public class StartupCompositionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StartupCompositionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Root_RedirectsToHealthEndpoint()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/api/health", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public void ServiceProvider_ResolvesImportantApiApplicationAndInfrastructureServices()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        AssertResolvable<IMediator>(services);
        AssertResolvable<WoongBlogDbContext>(services);
        AssertResolvable<HealthCheckService>(services);

        AssertResolvable<IPageCommandStore>(services);
        AssertResolvable<IPageQueryStore>(services);
        AssertResolvable<IBlogCommandStore>(services);
        AssertResolvable<IBlogQueryStore>(services);
        AssertResolvable<IWorkCommandStore>(services);
        AssertResolvable<IWorkQueryStore>(services);
        AssertResolvable<IWorkVideoCommandStore>(services);
        AssertResolvable<IWorkVideoCleanupStore>(services);
        AssertResolvable<IWorkVideoQueryStore>(services);
        AssertResolvable<ISiteSettingsCommandStore>(services);
        AssertResolvable<ISiteSettingsQueryStore>(services);
        AssertResolvable<IHomeQueryStore>(services);
        AssertResolvable<IAdminDashboardQueryStore>(services);
        AssertResolvable<IAdminMemberQueryStore>(services);
        AssertResolvable<IIdentityInteractionService>(services);
        AssertResolvable<IMediaAssetCommandStore>(services);
        AssertResolvable<IMediaAssetStorage>(services);
        AssertResolvable<IMediaAssetUploadPolicy>(services);
        AssertResolvable<IAiBatchTargetQueryStore>(services);
        AssertResolvable<IAiBatchJobQueryStore>(services);
        AssertResolvable<IAiBatchJobCommandStore>(services);
        AssertResolvable<IAiBatchJobSignal>(services);
        AssertResolvable<IAiRuntimeCapabilities>(services);
        AssertResolvable<IBlogAiFixService>(services);
        AssertResolvable<IAiBatchJobScheduler>(services);
        AssertResolvable<IAiBatchJobRunner>(services);
        AssertResolvable<IAiBatchJobItemProcessor>(services);
        AssertResolvable<IBlogFixApplyPolicy>(services);
        AssertResolvable<IWorkVideoCleanupService>(services);
        AssertResolvable<IWorkVideoStorageSelector>(services);
        AssertResolvable<IWorkVideoFileInspector>(services);
        AssertResolvable<IWorkVideoHlsWorkspace>(services);
        AssertResolvable<IWorkVideoHlsOutputPublisher>(services);
        AssertResolvable<IVideoTranscoder>(services);
        AssertResolvable<IWorkVideoPlaybackUrlBuilder>(services);

        var videoStorageServices = services.GetServices<IVideoObjectStorage>().Select(service => service.StorageType).ToArray();
        Assert.Contains(WorkVideoSourceTypes.Local, videoStorageServices);
        Assert.Contains(WorkVideoSourceTypes.R2, videoStorageServices);

        var hostedServiceTypes = services.GetServices<IHostedService>().Select(service => service.GetType()).ToArray();
        Assert.Contains(typeof(AiBatchJobProcessor), hostedServiceTypes);
        Assert.Contains(typeof(VideoStorageCleanupWorker), hostedServiceTypes);
    }

    [Fact]
    public void ServiceProvider_ResolvesImportantApplicationHandlersAndValidators()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        AssertResolvable<IValidator<CreateBlogRequest>>(services);
        AssertResolvable<IValidator<CreateBlogCommand>>(services);
        AssertResolvable<IRequestHandler<GetAdminPagesQuery, IReadOnlyList<AdminPageListItemDto>>>(services);
        AssertResolvable<IRequestHandler<GetPageBySlugQuery, PageDto?>>(services);
        AssertResolvable<IRequestHandler<UpdatePageCommand, AdminActionResult>>(services);
        AssertResolvable<IRequestHandler<GetAdminBlogsQuery, IReadOnlyList<AdminBlogListItemDto>>>(services);
        AssertResolvable<IRequestHandler<GetBlogsQuery, PagedBlogsDto>>(services);
        AssertResolvable<IRequestHandler<GetBlogBySlugQuery, BlogDetailDto?>>(services);
        AssertResolvable<IRequestHandler<CreateBlogCommand, AdminMutationResult>>(services);
        AssertResolvable<IRequestHandler<UpdateBlogCommand, AdminMutationResult?>>(services);
        AssertResolvable<IRequestHandler<DeleteBlogCommand, AdminActionResult>>(services);
        AssertResolvable<IRequestHandler<GetAdminWorksQuery, IReadOnlyList<AdminWorkListItemDto>>>(services);
        AssertResolvable<IRequestHandler<GetWorksQuery, PagedWorksDto>>(services);
        AssertResolvable<IRequestHandler<GetWorkBySlugQuery, WorkDetailDto?>>(services);
        AssertResolvable<IRequestHandler<CreateWorkCommand, AdminMutationResult>>(services);
        AssertResolvable<IRequestHandler<UpdateWorkCommand, AdminMutationResult?>>(services);
        AssertResolvable<IRequestHandler<DeleteWorkCommand, AdminActionResult>>(services);
        AssertResolvable<IRequestHandler<GetHomeQuery, HomeDto?>>(services);
        AssertResolvable<IRequestHandler<GetDashboardSummaryQuery, AdminDashboardSummaryDto>>(services);
        AssertResolvable<IRequestHandler<GetAdminMembersQuery, IReadOnlyList<AdminMemberListItemDto>>>(services);
        AssertResolvable<IRequestHandler<GetSiteSettingsQuery, SiteSettingsDto?>>(services);
        AssertResolvable<IRequestHandler<GetAdminSiteSettingsQuery, AdminSiteSettingsDto?>>(services);
        AssertResolvable<IRequestHandler<GetResumeQuery, ResumeDto?>>(services);
        AssertResolvable<IRequestHandler<UpdateSiteSettingsCommand, AdminActionResult>>(services);
        AssertResolvable<IRequestHandler<UploadMediaAssetCommand, MediaUploadResult>>(services);
        AssertResolvable<IRequestHandler<DeleteMediaAssetCommand, MediaDeleteResult>>(services);
        AssertResolvable<IRequestHandler<FixBlogHtmlCommand, AiActionResult<BlogFixResponse>>>(services);
        AssertResolvable<IRequestHandler<FixBlogBatchCommand, AiActionResult<BlogFixBatchResponse>>>(services);
        AssertResolvable<IRequestHandler<EnrichWorkHtmlCommand, AiActionResult<WorkEnrichResponse>>>(services);
        AssertResolvable<IRequestHandler<GetAiRuntimeConfigQuery, AiRuntimeConfigResponse>>(services);
        AssertResolvable<IRequestHandler<CreateBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobDetailResponse>>>(services);
        AssertResolvable<IRequestHandler<ListBlogFixBatchJobsQuery, BlogFixBatchJobListResponse>>(services);
        AssertResolvable<IRequestHandler<GetBlogFixBatchJobQuery, BlogFixBatchJobDetailResponse?>>(services);
        AssertResolvable<IRequestHandler<ApplyBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobDetailResponse>>>(services);
        AssertResolvable<IRequestHandler<CancelBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobSummaryResponse>>>(services);
        AssertResolvable<IRequestHandler<CancelQueuedBlogFixBatchJobsCommand, BlogFixBatchJobCancelQueuedResponse>>(services);
        AssertResolvable<IRequestHandler<ClearCompletedBlogFixBatchJobsCommand, BlogFixBatchJobClearCompletedResponse>>(services);
        AssertResolvable<IRequestHandler<RemoveBlogFixBatchJobCommand, AiActionResult<BlogFixBatchJobRemoveResponse>>>(services);
        AssertResolvable<IRequestHandler<IssueWorkVideoUploadCommand, WorkVideoResult<VideoUploadTargetResult>>>(services);
        AssertResolvable<IRequestHandler<UploadLocalWorkVideoCommand, WorkVideoResult<object>>>(services);
        AssertResolvable<IRequestHandler<ConfirmWorkVideoUploadCommand, WorkVideoResult<WorkVideosMutationResult>>>(services);
        AssertResolvable<IRequestHandler<AddYouTubeWorkVideoCommand, WorkVideoResult<WorkVideosMutationResult>>>(services);
        AssertResolvable<IRequestHandler<ReorderWorkVideosCommand, WorkVideoResult<WorkVideosMutationResult>>>(services);
        AssertResolvable<IRequestHandler<DeleteWorkVideoCommand, WorkVideoResult<WorkVideosMutationResult>>>(services);
        AssertResolvable<IRequestHandler<StartWorkVideoHlsJobCommand, WorkVideoResult<WorkVideosMutationResult>>>(services);
    }

    [Fact]
    public void AuthOptions_Use300MinuteSlidingExpiration_AndEightHourAbsoluteExpiration()
    {
        using var scope = _factory.Services.CreateScope();
        var authOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;

        Assert.Equal(300, authOptions.SlidingExpirationMinutes);
        Assert.Equal(8, authOptions.AbsoluteExpirationHours);
    }

    [Fact]
    public void Options_AreBoundForTestingStartup()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var authOptions = services.GetRequiredService<IOptions<AuthOptions>>().Value;
        var securityOptions = services.GetRequiredService<IOptions<SecurityOptions>>().Value;
        var proxyOptions = services.GetRequiredService<IOptions<ProxyOptions>>().Value;
        var aiOptions = services.GetRequiredService<IOptions<AiOptions>>().Value;
        var r2Options = services.GetRequiredService<IOptions<CloudflareR2Options>>().Value;
        var hlsOptions = services.GetRequiredService<IOptions<WorkVideoHlsOptions>>().Value;

        Assert.True(Path.IsPathRooted(authOptions.MediaRoot));
        Assert.True(Path.IsPathRooted(authOptions.DataProtectionKeysPath));
        Assert.False(authOptions.SecureCookies);
        Assert.False(securityOptions.UseHttpsRedirection);
        Assert.False(securityOptions.UseHsts);
        Assert.Contains("127.0.0.1", proxyOptions.KnownProxies);
        Assert.Equal("OpenAi", aiOptions.Provider);
        Assert.Contains("medium", aiOptions.CodexAllowedReasoningEfforts);
        Assert.False(r2Options.IsConfigured());
        Assert.True(hlsOptions.SegmentDurationSeconds > 0);
    }

    [Fact]
    public async Task HealthOpenApiAndRuntimeConfig_StartWithoutExternalServicesInTesting()
    {
        var client = _factory.CreateAuthenticatedClient();

        var healthResponse = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);
        var openApiResponse = await client.GetAsync("/api/openapi/v1.json", TestContext.Current.CancellationToken);
        var runtimeConfigResponse = await client.GetAsync("/api/admin/ai/runtime-config", TestContext.Current.CancellationToken);

        healthResponse.EnsureSuccessStatusCode();
        openApiResponse.EnsureSuccessStatusCode();
        runtimeConfigResponse.EnsureSuccessStatusCode();

        var health = await healthResponse.Content.ReadFromJsonAsync<HealthPayload>(TestContext.Current.CancellationToken);
        Assert.NotNull(health);
        Assert.Equal("ok", health!.Status);
        Assert.Equal("portfolio-api", health.Service);
        Assert.NotEqual(default, health.Timestamp);

        var openApiJson = await openApiResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"openapi\"", openApiJson, StringComparison.OrdinalIgnoreCase);

        using var runtimeConfigJson = await JsonDocument.ParseAsync(
            await runtimeConfigResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(runtimeConfigJson.RootElement.TryGetProperty("provider", out _));
        Assert.True(runtimeConfigJson.RootElement.TryGetProperty("availableProviders", out _));
    }

    [Fact]
    public async Task UnsafeAdminMutationWithoutCsrf_IsRejectedBeforeAuthorization()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/admin/site-settings",
            new { ownerName = "Missing CSRF" },
            TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CSRF", payload, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertResolvable<T>(IServiceProvider services)
        where T : notnull
    {
        Assert.NotNull(services.GetRequiredService<T>());
    }

    private sealed record HealthPayload(string Status, string Service, DateTimeOffset Timestamp);
}
