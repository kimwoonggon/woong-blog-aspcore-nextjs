using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http.Features;
using MediatR;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Auth;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Proxy;
using WoongBlog.Infrastructure.Security;
using WoongBlog.Infrastructure.Storage;
using WoongBlog.Infrastructure.Ai;
using WoongBlog.Infrastructure.LoadTesting;
using WoongBlog.Infrastructure.Persistence.Diagnostics;
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
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

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
        AssertResolvable<IRealLoadTestControlPlane>(services);
        AssertResolvable<IRealLoadTestRunRegistry>(services);

        var videoStorageServices = services.GetServices<IVideoObjectStorage>().Select(service => service.StorageType).ToArray();
        Assert.Contains(WorkVideoSourceTypes.Local, videoStorageServices);
        Assert.Contains(WorkVideoSourceTypes.R2, videoStorageServices);

        var hostedServiceTypes = services.GetServices<IHostedService>().Select(service => service.GetType()).ToArray();
        Assert.Contains(typeof(AiBatchJobProcessor), hostedServiceTypes);
        Assert.Contains(typeof(VideoStorageCleanupWorker), hostedServiceTypes);
    }

    [Fact]
    public void PersistenceRegistration_PoolsDbContextInstancesAndResetsStateAcrossScopes()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "InMemory",
                ["InMemoryDatabaseName"] = $"pooled-context-{Guid.NewGuid()}"
            })
            .Build();
        services.AddPersistenceInfrastructure(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        WoongBlogDbContext firstContext;
        using (var firstScope = provider.CreateScope())
        {
            firstContext = firstScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            firstContext.Blogs.Add(new Blog
            {
                Slug = "pooled-context-tracked-blog",
                Title = "Pooled Context Tracked Blog",
                Excerpt = "Tracked only to prove reset behavior.",
                ContentJson = "{}"
            });

            Assert.NotEmpty(firstContext.ChangeTracker.Entries());
        }

        using var secondScope = provider.CreateScope();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();

        Assert.Same(firstContext, secondContext);
        Assert.Empty(secondContext.ChangeTracker.Entries());
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
    public void JsonOptions_UseSourceGeneratedMetadataForPublicHotPathDtos()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var httpJsonOptions = services.GetRequiredService<IOptions<HttpJsonOptions>>().Value.SerializerOptions;
        var mvcJsonOptions = services.GetRequiredService<IOptions<MvcJsonOptions>>().Value.JsonSerializerOptions;

        AssertPublicSourceGeneratedJsonResolverConfigured(httpJsonOptions);
        AssertPublicSourceGeneratedJsonResolverConfigured(mvcJsonOptions);
    }

    [Fact]
    public void UploadLimits_AllowConfiguredWorkVideoMaximumBeforeAppValidation()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var kestrelOptions = services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;
        var formOptions = services.GetRequiredService<IOptions<FormOptions>>().Value;
        const long multipartOverheadBytes = 1L * 1024L * 1024L;

        Assert.NotNull(kestrelOptions.Limits.MaxRequestBodySize);
        Assert.True(
            kestrelOptions.Limits.MaxRequestBodySize >= WorkVideoPolicy.MaxVideoBytes + multipartOverheadBytes,
            $"Kestrel request limit {kestrelOptions.Limits.MaxRequestBodySize} must allow the {WorkVideoPolicy.MaxVideoBytes} byte video policy plus multipart overhead.");
        Assert.True(
            formOptions.MultipartBodyLengthLimit >= WorkVideoPolicy.MaxVideoBytes,
            $"Multipart body limit {formOptions.MultipartBodyLengthLimit} must allow the {WorkVideoPolicy.MaxVideoBytes} byte video policy.");
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
        var loadTestDiagnosticsResponse = await client.GetAsync("/api/admin/load-test/diagnostics", TestContext.Current.CancellationToken);

        healthResponse.EnsureSuccessStatusCode();
        openApiResponse.EnsureSuccessStatusCode();
        runtimeConfigResponse.EnsureSuccessStatusCode();
        loadTestDiagnosticsResponse.EnsureSuccessStatusCode();

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

        using var diagnosticsJson = await JsonDocument.ParseAsync(
            await loadTestDiagnosticsResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        var diagnostics = diagnosticsJson.RootElement;
        Assert.True(diagnostics.TryGetProperty("timestamp", out _));
        Assert.True(diagnostics.TryGetProperty("process", out var process));
        Assert.True(process.GetProperty("memoryBytes").GetInt64() > 0);
        Assert.True(process.GetProperty("processorCount").GetInt32() > 0);
        Assert.True(process.TryGetProperty("memoryLimitBytes", out var memoryLimitBytes));
        Assert.True(memoryLimitBytes.ValueKind is JsonValueKind.Number or JsonValueKind.Null);
        Assert.True(process.TryGetProperty("cpuQuotaCores", out var cpuQuotaCores));
        Assert.True(cpuQuotaCores.ValueKind is JsonValueKind.Number or JsonValueKind.Null);
        Assert.True(diagnostics.TryGetProperty("gc", out var gc));
        Assert.True(gc.GetProperty("heapSizeBytes").GetInt64() >= 0);
        Assert.True(gc.GetProperty("gen0Collections").GetInt32() >= 0);
        Assert.True(gc.GetProperty("gen1Collections").GetInt32() >= 0);
        Assert.True(gc.GetProperty("gen2Collections").GetInt32() >= 0);
        Assert.True(diagnostics.TryGetProperty("threadPool", out var threadPool));
        Assert.True(threadPool.GetProperty("maxWorkerThreads").GetInt32() > 0);
        Assert.True(threadPool.GetProperty("availableWorkerThreads").GetInt32() >= 0);
        Assert.True(threadPool.GetProperty("pendingWorkItemCount").GetInt64() >= 0);
        Assert.True(threadPool.GetProperty("completedWorkItemCount").GetInt64() >= 0);
        Assert.True(diagnostics.TryGetProperty("database", out var database));
        Assert.Equal("unavailable", database.GetProperty("status").GetString());
        Assert.True(database.GetProperty("timeoutCount").GetInt32() >= 0);
        Assert.True(database.TryGetProperty("errorCount", out var errorCount));
        Assert.True(errorCount.GetInt64() >= 0);
        Assert.True(database.TryGetProperty("commandLatency", out var commandLatency));
        Assert.True(commandLatency.TryGetProperty("sampleCount", out var commandSampleCount));
        Assert.True(commandSampleCount.GetInt32() >= 0);
        Assert.True(database.TryGetProperty("connectionOpenLatency", out var connectionOpenLatency));
        Assert.True(connectionOpenLatency.TryGetProperty("sampleCount", out var connectionSampleCount));
        Assert.True(connectionSampleCount.GetInt32() >= 0);
        Assert.True(database.TryGetProperty("slowQueryCount", out var slowQueryCount));
        Assert.True(slowQueryCount.GetInt64() >= 0);
        Assert.True(database.TryGetProperty("recentSlowQueries", out var recentSlowQueries));
        Assert.Equal(JsonValueKind.Array, recentSlowQueries.ValueKind);
        Assert.True(database.TryGetProperty("idleInTransactionConnections", out var idleInTransactionConnections));
        Assert.True(idleInTransactionConnections.ValueKind is JsonValueKind.Number or JsonValueKind.Null);
        Assert.True(database.TryGetProperty("pool", out var pool));
        Assert.False(string.IsNullOrWhiteSpace(pool.GetProperty("databaseProvider").GetString()));
        Assert.True(pool.GetProperty("dbContextPoolSize").GetInt32() > 0);
        Assert.True(pool.GetProperty("npgsqlMinimumPoolSize").ValueKind is JsonValueKind.Number or JsonValueKind.Null);
        Assert.True(pool.GetProperty("npgsqlMaximumPoolSize").ValueKind is JsonValueKind.Number or JsonValueKind.Null);
        Assert.False(string.IsNullOrWhiteSpace(pool.GetProperty("npgsqlPoolLimitSource").GetString()));
    }

    [Fact]
    public async Task LoadTestDiagnostics_RequiresAdminAuthorization()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/load-test/diagnostics", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RealLoadTestControlPlane_EndpointsRequireAdminAuthorization()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var csrfResponse = await client.GetAsync("/api/auth/csrf", TestContext.Current.CancellationToken);
        csrfResponse.EnsureSuccessStatusCode();
        using var csrfJson = await JsonDocument.ParseAsync(
            await csrfResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        var csrfToken = csrfJson.RootElement.GetProperty("requestToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(csrfToken));
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);

        var startResponse = await client.PostAsJsonAsync(
            "/api/admin/load-tests/real/start",
            new
            {
                scenario = "public-api-rps",
                runner = "k6",
                target = "public-api-mix",
                rate = 120,
                durationSeconds = 30,
                maxVus = 100
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, startResponse.StatusCode);

        var statusResponse = await client.GetAsync("/api/admin/load-tests/real/non-existent-run", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, statusResponse.StatusCode);

        var metricsResponse = await client.GetAsync("/api/admin/load-tests/real/non-existent-run/metrics", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, metricsResponse.StatusCode);

        var stopResponse = await client.PostAsync(
            "/api/admin/load-tests/real/non-existent-run/stop",
            content: null,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, stopResponse.StatusCode);
    }

    [Fact]
    public async Task RealLoadTestControlPlane_StartStatusMetricsAndStop_HappyPath_WhenRealRunnerDisabled_ForcesFakeRunner()
    {
        var client = _factory.CreateAuthenticatedClient();
        var startResponse = await client.PostAsJsonAsync(
            "/api/admin/load-tests/real/start",
            new
            {
                scenario = "public-api-rps",
                runner = "k6",
                target = "public-api-mix",
                rate = 120,
                peakRate = 180,
                durationSeconds = 120,
                maxVus = 1000,
                startVus = 20,
                targets = new[]
                {
                    new
                    {
                        id = "work-list",
                        label = "Work list",
                        path = "/api/public/works?page=1&pageSize=12",
                        group = "work"
                    },
                    new
                    {
                        id = "study-read",
                        label = "Study read",
                        path = "/api/public/blogs/seeded-study",
                        group = "study"
                    }
                }
            },
            TestContext.Current.CancellationToken);

        startResponse.EnsureSuccessStatusCode();
        var startPayload = await startResponse.Content.ReadFromJsonAsync<RealLoadTestStartResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(startPayload);
        Assert.False(string.IsNullOrWhiteSpace(startPayload!.RunId));
        Assert.Matches(@"^\d{8}-\d{6}-public-api-rps-[0-9a-f]{8}$", startPayload.RunId);
        Assert.Equal("running", startPayload.Status);
        Assert.Equal("fake", startPayload.Runner);
        Assert.Equal("public-api-rps", startPayload.Scenario);
        Assert.Equal(2, startPayload.Targets.Count);
        Assert.Contains(startPayload.Targets, target => target.Id == "study-read" && target.Path == "/api/public/blogs/seeded-study");
        Assert.NotEqual(default, startPayload.StartedAtUtc);

        var statusResponse = await client.GetAsync($"/api/admin/load-tests/real/{startPayload.RunId}", TestContext.Current.CancellationToken);
        statusResponse.EnsureSuccessStatusCode();
        var statusPayload = await statusResponse.Content.ReadFromJsonAsync<RealLoadTestStatusResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(statusPayload);
        Assert.Equal(startPayload.RunId, statusPayload!.RunId);
        Assert.Equal("running", statusPayload.Status);
        Assert.Equal("fake", statusPayload.Runner);
        Assert.Equal("public-api-rps", statusPayload.Scenario);
        Assert.True(statusPayload.ElapsedSeconds >= 0);
        Assert.True(statusPayload.TotalRequests >= 0);
        Assert.True(statusPayload.FailedRequests >= 0);
        Assert.Equal(2, statusPayload.Targets.Count);

        await Task.Delay(TimeSpan.FromMilliseconds(700), TestContext.Current.CancellationToken);
        var metricsResponse = await client.GetAsync($"/api/admin/load-tests/real/{startPayload.RunId}/metrics", TestContext.Current.CancellationToken);
        metricsResponse.EnsureSuccessStatusCode();
        var metricsPayload = await metricsResponse.Content.ReadFromJsonAsync<RealLoadTestMetricsResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(metricsPayload);
        Assert.Equal(startPayload.RunId, metricsPayload!.RunId);
        Assert.Equal("running", metricsPayload.Status);
        Assert.True(metricsPayload.TotalRequests >= 0);
        Assert.True(metricsPayload.CurrentRps >= 0);
        Assert.NotNull(metricsPayload.StatusCounts);
        Assert.NotNull(metricsPayload.Metrics);
        Assert.NotEmpty(metricsPayload.Metrics);
        Assert.NotEmpty(metricsPayload.TargetMetrics);
        Assert.Contains(metricsPayload.TargetMetrics, target => target.TargetId == "study-read" && target.RequestCount > 0);
        Assert.NotNull(metricsPayload.LatencyBreakdown);
        Assert.True(metricsPayload.LatencyBreakdown.P95Ms >= 0);
        Assert.True(metricsPayload.LatencyBreakdown.AppElapsedP95Ms >= 0);
        Assert.NotNull(metricsPayload.Diagnostics);
        Assert.NotEmpty(metricsPayload.Diagnostics);
        var runDiagnostics = metricsPayload.Diagnostics[^1];
        Assert.True(runDiagnostics.Process.MemoryBytes > 0);
        Assert.True(runDiagnostics.Process.ProcessorCount > 0);
        Assert.True(runDiagnostics.Gc.HeapSizeBytes >= 0);
        Assert.True(runDiagnostics.ThreadPool.MaxWorkerThreads > 0);
        Assert.True(runDiagnostics.Database.CommandLatency.SampleCount >= 0);
        Assert.True(runDiagnostics.Database.ConnectionOpenLatency.SampleCount >= 0);
        Assert.Contains(metricsPayload.Metrics, metric => metric.Diagnostics is not null);
        Assert.Equal(
            metricsPayload.Metrics.Count(static metric => metric.Diagnostics is not null),
            metricsPayload.Diagnostics.Count);

        var stopResponse = await client.PostAsync(
            $"/api/admin/load-tests/real/{startPayload.RunId}/stop",
            content: null,
            TestContext.Current.CancellationToken);
        stopResponse.EnsureSuccessStatusCode();
        var stopPayload = await stopResponse.Content.ReadFromJsonAsync<RealLoadTestStopResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(stopPayload);
        Assert.Equal(startPayload.RunId, stopPayload!.RunId);
        Assert.Equal("stopped", stopPayload.Status);
        Assert.NotNull(stopPayload.StoppedAtUtc);
    }

    [Fact]
    public async Task RealLoadTestControlPlane_StartRejectsSpikePeakRateBelowBaseRate()
    {
        var client = _factory.CreateAuthenticatedClient();
        var startResponse = await client.PostAsJsonAsync(
            "/api/admin/load-tests/real/start",
            new
            {
                scenario = "public-api-spike",
                runner = "k6",
                target = "public-api-mix",
                rate = 50,
                peakRate = 25,
                durationSeconds = 30,
                maxVus = 50,
                startVus = 5
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, startResponse.StatusCode);

        using var payload = await JsonDocument.ParseAsync(
            await startResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("PeakRate", payload.RootElement.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RealLoadTestControlPlane_RealRunnerDisabledFallsBackToFakeRunner()
    {
        await using var disabledFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LoadTesting:UseFakeRunnerForTests"] = "false",
                    ["LoadTesting:RealRunnerEnabled"] = "false"
                });
            });
        });

        var client = disabledFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "admin");

        var csrfResponse = await client.GetAsync("/api/auth/csrf", TestContext.Current.CancellationToken);
        csrfResponse.EnsureSuccessStatusCode();

        using var csrfPayload = await JsonDocument.ParseAsync(
            await csrfResponse.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);

        var csrfToken = csrfPayload.RootElement.GetProperty("requestToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(csrfToken));
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);

        var startResponse = await client.PostAsJsonAsync(
            "/api/admin/load-tests/real/start",
            new
            {
                scenario = "public-api-rps",
                runner = "k6",
                target = "public-api-mix",
                rate = 120,
                durationSeconds = 30,
                maxVus = 100
            },
            TestContext.Current.CancellationToken);

        startResponse.EnsureSuccessStatusCode();
        var startPayload = await startResponse.Content.ReadFromJsonAsync<RealLoadTestStartResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(startPayload);
        Assert.Equal("fake", startPayload!.Runner);
        Assert.Equal("running", startPayload.Status);
    }

    [Fact]
    public async Task LoadTestDiagnostics_WhenDbCollectorThrows_ReturnsErrorDatabasePayload()
    {
        await using var throwingFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDatabaseDiagnosticsCollector>();
                services.AddSingleton<IDatabaseDiagnosticsCollector, ThrowingDatabaseDiagnosticsCollector>();
            });
        });

        var client = throwingFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "admin");

        var response = await client.GetAsync("/api/admin/load-test/diagnostics", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        using var diagnosticsJson = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);

        var database = diagnosticsJson.RootElement.GetProperty("database");
        Assert.Equal("error", database.GetProperty("status").GetString());
        Assert.Equal("collector_failure", database.GetProperty("errorCategory").GetString());
        Assert.Contains("simulated collector failure", database.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
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

    private static void AssertPublicSourceGeneratedJsonResolverConfigured(JsonSerializerOptions options)
    {
        var resolver = Assert.Single(
            options.TypeInfoResolverChain,
            candidate => candidate.GetType().Name == "WoongBlogApiJsonSerializerContext");

        var publicHotPathTypes = new[]
        {
            typeof(HomeDto),
            typeof(PagedBlogsDto),
            typeof(BlogDetailDto),
            typeof(PagedWorksDto),
            typeof(WorkDetailDto),
            typeof(PageDto),
            typeof(SiteSettingsDto),
            typeof(WoongBlog.Infrastructure.LoadTesting.RealLoadTestStatusResponse),
            typeof(WoongBlog.Infrastructure.LoadTesting.RealLoadTestMetricsResponse),
            typeof(WoongBlog.Infrastructure.LoadTesting.LoadTestDatabasePoolDiagnostics)
        };

        foreach (var type in publicHotPathTypes)
        {
            Assert.NotNull(resolver.GetTypeInfo(type, options));
        }
    }

    private sealed record HealthPayload(string Status, string Service, DateTimeOffset Timestamp);

    private sealed record RealLoadTestStartResponse(
        string RunId,
        string Status,
        string Runner,
        string Scenario,
        IReadOnlyList<RealLoadTestTargetResponse> Targets,
        DateTimeOffset StartedAtUtc);

    private sealed record RealLoadTestStatusResponse(
        string RunId,
        string Status,
        string Runner,
        string Scenario,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset? EndedAtUtc,
        double ElapsedSeconds,
        long TotalRequests,
        long FailedRequests,
        double ErrorRate,
        double CurrentRps,
        double AverageRps,
        double P50Ms,
        double P95Ms,
        double P99Ms,
        double MaxMs,
        IReadOnlyDictionary<string, long> StatusCounts,
        IReadOnlyList<RealLoadTestTargetResponse> Targets);

    private sealed record RealLoadTestMetricPoint(
        DateTimeOffset TimestampUtc,
        double ElapsedSeconds,
        long TotalRequests,
        long FailedRequests,
        double CurrentRps,
        double AverageRps,
        double P95Ms,
        double P99Ms,
        double MaxMs,
        IReadOnlyDictionary<string, long> StatusCounts,
        RealLoadTestLatencyBreakdownResponse LatencyBreakdown,
        IReadOnlyList<RealLoadTestTargetMetricsResponse> TargetMetrics,
        RealLoadTestDiagnosticsResponse? Diagnostics);

    private sealed record RealLoadTestMetricsResponse(
        string RunId,
        string Status,
        long TotalRequests,
        long FailedRequests,
        double CurrentRps,
        double AverageRps,
        double P95Ms,
        double P99Ms,
        double MaxMs,
        IReadOnlyDictionary<string, long> StatusCounts,
        RealLoadTestLatencyBreakdownResponse LatencyBreakdown,
        IReadOnlyList<RealLoadTestTargetMetricsResponse> TargetMetrics,
        IReadOnlyList<RealLoadTestMetricPoint> Metrics,
        IReadOnlyList<RealLoadTestDiagnosticsResponse> Diagnostics);

    private sealed record RealLoadTestStopResponse(
        string RunId,
        string Status,
        DateTimeOffset? StoppedAtUtc);

    private sealed record RealLoadTestTargetResponse(
        string Id,
        string Label,
        string Path,
        string Group);

    private sealed record RealLoadTestLatencyBreakdownResponse(
        double MinMs,
        double P50Ms,
        double P95Ms,
        double P99Ms,
        double MaxMs,
        double AppElapsedP95Ms,
        double? NginxRequestTimeP95Ms,
        double? NginxUpstreamP95Ms);

    private sealed record RealLoadTestTargetMetricsResponse(
        string TargetId,
        string TargetLabel,
        string TargetPath,
        string Group,
        long RequestCount,
        long SuccessCount,
        long FailureCount,
        double P95Ms,
        double? ResponseBytesP95,
        double? ReceiveP95Ms,
        IReadOnlyDictionary<string, long> StatusCounts);

    private sealed record RealLoadTestDiagnosticsResponse(
        DateTimeOffset Timestamp,
        RealLoadTestProcessDiagnosticsResponse Process,
        RealLoadTestGcDiagnosticsResponse Gc,
        RealLoadTestThreadPoolDiagnosticsResponse ThreadPool,
        RealLoadTestDatabaseDiagnosticsResponse Database);

    private sealed record RealLoadTestProcessDiagnosticsResponse(
        long MemoryBytes,
        int ProcessorCount);

    private sealed record RealLoadTestGcDiagnosticsResponse(
        long HeapSizeBytes,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        double TimeInGcPercent);

    private sealed record RealLoadTestThreadPoolDiagnosticsResponse(
        int WorkerThreads,
        long PendingWorkItemCount,
        long CompletedWorkItemCount,
        int AvailableWorkerThreads,
        int MaxWorkerThreads);

    private sealed record RealLoadTestDatabaseDiagnosticsResponse(
        string Status,
        double? LatencyMs,
        int? OpenConnections,
        int? ActiveConnections,
        int? IdleConnections,
        int? IdleInTransactionConnections,
        RealLoadTestDatabaseLatencyResponse CommandLatency,
        RealLoadTestDatabaseLatencyResponse ConnectionOpenLatency,
        long SlowQueryCount,
        IReadOnlyList<RealLoadTestSlowQueryResponse> RecentSlowQueries,
        long TimeoutCount,
        long ErrorCount,
        string? Error,
        string? ErrorCategory);

    private sealed record RealLoadTestDatabaseLatencyResponse(
        int SampleCount,
        double? P50Ms,
        double? P95Ms,
        double? P99Ms);

    private sealed record RealLoadTestSlowQueryResponse(
        DateTimeOffset CapturedAt,
        double DurationMs,
        string SqlPreview,
        string? ErrorCategory);

    private sealed class ThrowingDatabaseDiagnosticsCollector : IDatabaseDiagnosticsCollector
    {
        public void RecordCommand(TimeSpan duration, string? commandText, Exception? exception = null)
        {
        }

        public void RecordConnectionOpen(TimeSpan duration, Exception? exception = null)
        {
        }

        public DatabaseDiagnosticsMetricsSnapshot CaptureSnapshot()
        {
            throw new InvalidOperationException("simulated collector failure");
        }
    }
}
