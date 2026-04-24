using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.AI;
using WoongBlog.Application.Modules.AI.Abstractions;
using WoongBlog.Application.Modules.AI.BatchJobs;
using WoongBlog.Application.Modules.AI.RuntimeConfig;
using WoongBlog.Infrastructure.Ai;
using WoongBlog.Infrastructure.Modules.AI.Persistence;
using WoongBlog.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public sealed class AiBatchRuntimeComponentTests
{
    [Fact]
    public async Task CreateBlogFixBatchJobCommandHandler_PersistsQueuedJobAndPendingItemsForSelectedTargets()
    {
        await using var dbContext = CreateDbContext();
        var older = AddBlog(dbContext, "older", "Older target", "<p>older</p>", DateTimeOffset.UtcNow.AddMinutes(-10));
        var newer = AddBlog(dbContext, "newer", "Newer target", "<p>newer</p>", DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
        var store = new AiBlogFixBatchStore(dbContext);
        var signal = new RecordingBatchJobSignal();
        var handler = new CreateBlogFixBatchJobCommandHandler(
            store,
            store,
            store,
            signal,
            new FixedRuntimeCapabilities(["openai", "codex"]),
            Options.Create(CreateAiOptions(provider: "Codex")));

        var result = await handler.Handle(new CreateBlogFixBatchJobCommand(
            [older.Id, newer.Id],
            All: false,
            SelectionMode: "selected",
            SelectionLabel: "manual selection",
            AutoApply: false,
            WorkerCount: 12,
            Provider: "codex",
            CodexModel: "gpt-5.3-codex",
            CodexReasoningEffort: "high",
            CustomPrompt: " Keep this prompt. "), CancellationToken.None);

        Assert.Equal(AiActionStatus.Ok, result.Status);
        Assert.Equal(1, signal.NotifyCount);
        Assert.NotNull(result.Value);
        Assert.Equal("queued", result.Value!.Status);
        Assert.Equal("codex", result.Value.Provider);
        Assert.Equal("gpt-5.3-codex", result.Value.Model);
        Assert.Equal("high", result.Value.ReasoningEffort);
        Assert.Equal("Keep this prompt.", result.Value.CustomPrompt);
        Assert.Equal(8, result.Value.WorkerCount);
        Assert.Equal(2, result.Value.TotalCount);

        var persistedJob = await dbContext.AiBatchJobs.SingleAsync();
        var persistedItems = await dbContext.AiBatchJobItems
            .OrderBy(item => item.CreatedAt)
            .ToListAsync();
        Assert.Equal(persistedJob.Id, result.Value.JobId);
        Assert.Equal("queued", persistedJob.Status);
        Assert.Equal("manual selection", persistedJob.SelectionLabel);
        Assert.Equal(["Newer target", "Older target"], persistedItems.Select(item => item.Title).ToArray());
        Assert.All(persistedItems, item => Assert.Equal("pending", item.Status));
    }

    [Fact]
    public async Task AiBlogFixBatchStore_SelectsRequestedTargetsOrAllTargetsDeterministically()
    {
        await using var dbContext = CreateDbContext();
        var oldest = AddBlog(dbContext, "oldest", "Oldest", "<p>oldest</p>", DateTimeOffset.UtcNow.AddMinutes(-20));
        var middle = AddBlog(dbContext, "middle", "Middle", "<p>middle</p>", DateTimeOffset.UtcNow.AddMinutes(-10));
        var newest = AddBlog(dbContext, "newest", "Newest", "<p>newest</p>", DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
        var store = new AiBlogFixBatchStore(dbContext);

        var selected = await store.GetBlogTargetsAsync(all: false, [oldest.Id, newest.Id], CancellationToken.None);
        var all = await store.GetBlogTargetsAsync(all: true, [oldest.Id], CancellationToken.None);

        Assert.Equal([newest.Id, oldest.Id], selected.Select(target => target.Id).ToArray());
        Assert.Equal([newest.Id, middle.Id, oldest.Id], all.Select(target => target.Id).ToArray());
    }

    [Fact]
    public async Task AiBatchJobScheduler_ResetRunningJobs_RequeuesInterruptedJobs()
    {
        await using var dbContext = CreateDbContext();
        var job = AddJob(dbContext, AiBatchJobStates.Running);
        job.StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        await dbContext.SaveChangesAsync();
        var store = new AiBlogFixBatchStore(dbContext);
        var scheduler = new AiBatchJobScheduler(store, store, new NoopBatchJobRunner());

        await scheduler.ResetRunningJobsAsync(CancellationToken.None);

        Assert.Equal(AiBatchJobStates.Queued, job.Status);
        Assert.NotNull(job.StartedAt);
    }

    [Fact]
    public async Task AiBatchJobScheduler_ProcessQueuedJobsUntilEmpty_CompletesSuccessfulJobAndPersistsResult()
    {
        await using var dbContext = CreateDbContext();
        var blog = AddBlog(dbContext, "success", "Successful blog", "<p>original</p>", DateTimeOffset.UtcNow);
        var job = AddJob(dbContext, AiBatchJobStates.Queued, workerCount: 1);
        AddItem(dbContext, job, blog);
        await dbContext.SaveChangesAsync();
        string? observedItemStatus = null;
        using var provider = CreateBatchServiceProvider(
            dbContext,
            new CallbackBlogAiFixService(async (_, options) =>
            {
                observedItemStatus = await dbContext.AiBatchJobItems
                    .AsNoTracking()
                    .Where(item => item.JobId == job.Id)
                    .Select(item => item.Status)
                    .SingleAsync();
                return new BlogAiFixResult("<p>fixed result</p>", options?.Provider ?? "openai", options?.CodexModel ?? "model", options?.CodexReasoningEffort);
            }));
        using var scope = provider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IAiBatchJobScheduler>();

        await scheduler.ProcessQueuedJobsUntilEmptyAsync(CancellationToken.None);

        dbContext.ChangeTracker.Clear();
        var persistedJob = await dbContext.AiBatchJobs.SingleAsync();
        var persistedItem = await dbContext.AiBatchJobItems.SingleAsync();
        Assert.Equal(AiBatchJobStates.Completed, persistedJob.Status);
        Assert.Equal(1, persistedJob.ProcessedCount);
        Assert.Equal(1, persistedJob.SucceededCount);
        Assert.Equal(0, persistedJob.FailedCount);
        Assert.NotNull(persistedJob.StartedAt);
        Assert.NotNull(persistedJob.FinishedAt);
        Assert.Equal(AiBatchJobItemStates.Running, observedItemStatus);
        Assert.Equal(AiBatchJobItemStates.Succeeded, persistedItem.Status);
        Assert.Equal("<p>fixed result</p>", persistedItem.FixedHtml);
        Assert.Equal("openai", persistedItem.Provider);
        Assert.Null(persistedItem.Error);
        Assert.NotNull(persistedItem.StartedAt);
        Assert.NotNull(persistedItem.FinishedAt);
    }

    [Fact]
    public async Task AiBatchJobScheduler_ProcessQueuedJobsUntilEmpty_RepresentsPartialFailuresWithoutCorruptingOtherBlogs()
    {
        await using var dbContext = CreateDbContext();
        var successBlog = AddBlog(dbContext, "success", "Success", "<p>safe</p>", DateTimeOffset.UtcNow);
        var failingBlog = AddBlog(dbContext, "failure", "Failure", "<p>please fail</p>", DateTimeOffset.UtcNow.AddMinutes(-1));
        var unrelatedBlog = AddBlog(dbContext, "unrelated", "Unrelated", "<p>untouched</p>", DateTimeOffset.UtcNow.AddMinutes(-2));
        var originalFailingContent = failingBlog.ContentJson;
        var originalUnrelatedContent = unrelatedBlog.ContentJson;
        var job = AddJob(dbContext, AiBatchJobStates.Queued, autoApply: true, workerCount: 1);
        AddItem(dbContext, job, successBlog);
        AddItem(dbContext, job, failingBlog);
        await dbContext.SaveChangesAsync();
        using var provider = CreateBatchServiceProvider(
            dbContext,
            new CallbackBlogAiFixService((html, options) =>
            {
                if (html.Contains("fail", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("runtime failed for target");
                }

                return Task.FromResult(new BlogAiFixResult("<p>fixed safe</p>", options?.Provider ?? "openai", options?.CodexModel ?? "model", options?.CodexReasoningEffort));
            }));
        using var scope = provider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IAiBatchJobScheduler>();

        await scheduler.ProcessQueuedJobsUntilEmptyAsync(CancellationToken.None);

        dbContext.ChangeTracker.Clear();
        var persistedJob = await dbContext.AiBatchJobs.SingleAsync();
        var items = await dbContext.AiBatchJobItems.OrderBy(item => item.Title).ToListAsync();
        var reloadedSuccess = await dbContext.Blogs.SingleAsync(blog => blog.Id == successBlog.Id);
        var reloadedFailure = await dbContext.Blogs.SingleAsync(blog => blog.Id == failingBlog.Id);
        var reloadedUnrelated = await dbContext.Blogs.SingleAsync(blog => blog.Id == unrelatedBlog.Id);

        Assert.Equal(AiBatchJobStates.Completed, persistedJob.Status);
        Assert.Equal(2, persistedJob.ProcessedCount);
        Assert.Equal(1, persistedJob.SucceededCount);
        Assert.Equal(1, persistedJob.FailedCount);
        Assert.Contains("fixed safe", reloadedSuccess.ContentJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(originalFailingContent, reloadedFailure.ContentJson);
        Assert.Equal(originalUnrelatedContent, reloadedUnrelated.ContentJson);
        Assert.Contains(items, item => item.Title == "Success" && item.Status == AiBatchJobItemStates.Applied && item.Error is null);
        Assert.Contains(items, item => item.Title == "Failure" && item.Status == AiBatchJobItemStates.Failed && item.Error == "runtime failed for target");
    }

    [Fact]
    public async Task AiBatchJobScheduler_ProcessQueuedJobsUntilEmpty_MarksJobFailedWhenEveryRuntimeCallFails()
    {
        await using var dbContext = CreateDbContext();
        var blog = AddBlog(dbContext, "failure", "Failure", "<p>failure</p>", DateTimeOffset.UtcNow);
        var job = AddJob(dbContext, AiBatchJobStates.Queued, workerCount: 1);
        AddItem(dbContext, job, blog);
        await dbContext.SaveChangesAsync();
        using var provider = CreateBatchServiceProvider(
            dbContext,
            new CallbackBlogAiFixService((_, _) => throw new InvalidOperationException("runtime unavailable")));
        using var scope = provider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IAiBatchJobScheduler>();

        await scheduler.ProcessQueuedJobsUntilEmptyAsync(CancellationToken.None);

        dbContext.ChangeTracker.Clear();
        var persistedJob = await dbContext.AiBatchJobs.SingleAsync();
        var persistedItem = await dbContext.AiBatchJobItems.SingleAsync();
        Assert.Equal(AiBatchJobStates.Failed, persistedJob.Status);
        Assert.Equal(1, persistedJob.ProcessedCount);
        Assert.Equal(0, persistedJob.SucceededCount);
        Assert.Equal(1, persistedJob.FailedCount);
        Assert.Equal(AiBatchJobItemStates.Failed, persistedItem.Status);
        Assert.Equal("runtime unavailable", persistedItem.Error);
        Assert.Null(persistedItem.FixedHtml);
    }

    [Fact]
    public async Task GetAiRuntimeConfigQueryHandler_ReturnsStableDefaultsAndPromptMetadata()
    {
        var options = CreateAiOptions(provider: "Codex");
        options.BatchConcurrency = 4;
        options.BatchCompletedRetentionDays = 9;
        var handler = new GetAiRuntimeConfigQueryHandler(
            Options.Create(options),
            new FixedRuntimeCapabilities(["openai", "codex"], "blog prompt", "work prompt"));

        var response = await handler.Handle(new GetAiRuntimeConfigQuery(), CancellationToken.None);

        Assert.Equal("codex", response.Provider);
        Assert.Equal(["openai", "codex"], response.AvailableProviders);
        Assert.Equal("gpt-5.4", response.DefaultModel);
        Assert.Equal("gpt-5.4", response.CodexModel);
        Assert.Equal("medium", response.CodexReasoningEffort);
        Assert.Equal(["gpt-5.4", "gpt-5.3-codex", "gpt-5.3-codex-spark"], response.AllowedCodexModels);
        Assert.Equal(["low", "medium", "high", "xhigh"], response.AllowedCodexReasoningEfforts);
        Assert.Equal(4, response.BatchConcurrency);
        Assert.Equal(9, response.BatchCompletedRetentionDays);
        Assert.Equal("blog prompt", response.DefaultSystemPrompt);
        Assert.Equal("blog prompt", response.DefaultBlogFixPrompt);
        Assert.Equal("work prompt", response.DefaultWorkEnrichPrompt);
    }

    [Fact]
    public async Task GetAiRuntimeConfigQueryHandler_FallsBackSafelyWhenConfiguredProviderIsUnavailable()
    {
        var options = CreateAiOptions(provider: "Codex");
        options.OpenAiModel = "openai-fallback-model";
        var handler = new GetAiRuntimeConfigQueryHandler(
            Options.Create(options),
            new FixedRuntimeCapabilities(["openai"]));

        var response = await handler.Handle(new GetAiRuntimeConfigQuery(), CancellationToken.None);

        Assert.Equal("openai", response.Provider);
        Assert.Equal("openai-fallback-model", response.DefaultModel);
        Assert.Equal(["openai"], response.AvailableProviders);
    }

    [Fact]
    public void AiOptionsPostConfigure_LoadsEnvironmentStyleOverridesAndStableDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI_PROVIDER"] = "Codex",
                ["CODEX_ARGUMENTS"] = "exec --json \"two words\"",
                ["CODEX_TIMEOUT_MS"] = "12345",
                ["AI_BATCH_CONCURRENCY"] = "5",
                ["AI_BATCH_COMPLETED_RETENTION_DAYS"] = "0",
                ["CODEX_ALLOWED_MODELS"] = "gpt-5.4,gpt-5.3-codex",
                ["CODEX_ALLOWED_REASONING_EFFORTS"] = "low,high"
            })
            .Build();
        var options = new AiOptions();

        new AiOptionsPostConfigure(configuration).PostConfigure(name: null, options);

        Assert.Equal("Codex", options.Provider);
        Assert.Equal(["exec", "--json", "two words"], options.CodexArguments);
        Assert.Equal(12345, options.CodexTimeoutMs);
        Assert.Equal(5, options.BatchConcurrency);
        Assert.Equal(0, options.BatchCompletedRetentionDays);
        Assert.Equal(["gpt-5.4", "gpt-5.3-codex"], options.CodexAllowedModels);
        Assert.Equal(["low", "high"], options.CodexAllowedReasoningEfforts);
    }

    [Fact]
    public void AiOptionsValidator_FailsSafelyForInvalidRuntimeConfiguration()
    {
        var options = new AiOptions
        {
            Provider = "invalid",
            CodexCommand = "",
            CodexTimeoutMs = 0,
            BatchConcurrency = 0,
            BatchCompletedRetentionDays = -1,
            AzureOpenAiEndpoint = "not absolute",
            CodexAllowedModels = ["gpt-5.4", ""],
            CodexAllowedReasoningEfforts = ["medium", "extreme"]
        };

        var result = new AiOptionsValidator().Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Contains("Ai:Provider must be one of OpenAi, Azure, or Codex.", result.Failures);
        Assert.Contains("Ai:CodexTimeoutMs must be greater than 0.", result.Failures);
        Assert.Contains("Ai:BatchConcurrency must be greater than 0.", result.Failures);
        Assert.Contains("Ai:BatchCompletedRetentionDays must be 0 or greater.", result.Failures);
        Assert.Contains("Ai:AzureOpenAiEndpoint must be an absolute URI when provided.", result.Failures);
        Assert.Contains("Ai:CodexAllowedModels cannot contain blank entries.", result.Failures);
        Assert.Contains("Ai:CodexAllowedReasoningEfforts contains an unsupported value.", result.Failures);
    }

    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    private static ServiceProvider CreateBatchServiceProvider(WoongBlogDbContext dbContext, IBlogAiFixService aiFixService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddSingleton(Options.Create(CreateAiOptions()));
        services.AddScoped<AiBlogFixBatchStore>();
        services.AddScoped<IAiBatchTargetQueryStore>(serviceProvider => serviceProvider.GetRequiredService<AiBlogFixBatchStore>());
        services.AddScoped<IAiBatchJobQueryStore>(serviceProvider => serviceProvider.GetRequiredService<AiBlogFixBatchStore>());
        services.AddScoped<IAiBatchJobCommandStore>(serviceProvider => serviceProvider.GetRequiredService<AiBlogFixBatchStore>());
        services.AddSingleton<IAiBatchJobItemDispatcher, AiBatchJobItemDispatcher>();
        services.AddScoped<IAiBatchJobItemProcessor, AiBatchJobItemProcessor>();
        services.AddScoped<IAiBatchJobRunner, AiBatchJobRunner>();
        services.AddScoped<IAiBatchJobScheduler, AiBatchJobScheduler>();
        services.AddSingleton<IBlogFixApplyPolicy, BlogFixApplyPolicy>();
        services.AddSingleton(aiFixService);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static Blog AddBlog(
        WoongBlogDbContext dbContext,
        string slug,
        string title,
        string html,
        DateTimeOffset updatedAt)
    {
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = title,
            Excerpt = title,
            ContentJson = $$"""{"html":{{JsonSerializer.Serialize(html)}}}""",
            Published = true,
            PublishedAt = updatedAt,
            CreatedAt = updatedAt.AddMinutes(-5),
            UpdatedAt = updatedAt
        };

        dbContext.Blogs.Add(blog);
        return blog;
    }

    private static AiBatchJob AddJob(
        WoongBlogDbContext dbContext,
        string status,
        bool autoApply = false,
        int? workerCount = null)
    {
        var job = new AiBatchJob
        {
            Id = Guid.NewGuid(),
            TargetType = "blog",
            Status = status,
            SelectionMode = "selected",
            SelectionLabel = "component test",
            SelectionKey = $"component:{Guid.NewGuid():N}",
            AutoApply = autoApply,
            WorkerCount = workerCount,
            Provider = "openai",
            Model = "gpt-4o",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AiBatchJobs.Add(job);
        return job;
    }

    private static void AddItem(WoongBlogDbContext dbContext, AiBatchJob job, Blog blog)
    {
        var item = new AiBatchJobItem
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            EntityId = blog.Id,
            Title = blog.Title,
            Status = AiBatchJobItemStates.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.AiBatchJobItems.Add(item);
    }

    private static AiOptions CreateAiOptions(string provider = "OpenAi")
    {
        return new AiOptions
        {
            Provider = provider,
            OpenAiModel = "gpt-4o",
            CodexCommand = "codex",
            CodexModel = "gpt-5.4",
            CodexReasoningEffort = "medium",
            CodexTimeoutMs = 5000,
            BatchConcurrency = 2,
            BatchCompletedRetentionDays = 3,
            CodexAllowedModels = ["gpt-5.4", "gpt-5.3-codex", "gpt-5.3-codex-spark"],
            CodexAllowedReasoningEfforts = ["low", "medium", "high", "xhigh"]
        };
    }

    private sealed class RecordingBatchJobSignal : IAiBatchJobSignal
    {
        public int NotifyCount { get; private set; }

        public void Notify()
        {
            NotifyCount += 1;
        }
    }

    private sealed class FixedRuntimeCapabilities(
        IReadOnlyList<string> availableProviders,
        string blogPrompt = "default blog prompt",
        string workPrompt = "default work prompt") : IAiRuntimeCapabilities
    {
        public IReadOnlyList<string> GetAvailableProviders() => availableProviders;

        public string GetDefaultBlogFixPrompt() => blogPrompt;

        public string GetDefaultWorkEnrichPrompt() => workPrompt;
    }

    private sealed class NoopBatchJobRunner : IAiBatchJobRunner
    {
        public Task RunAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class CallbackBlogAiFixService(
        Func<string, AiFixRequestOptions?, Task<BlogAiFixResult>> callback) : IBlogAiFixService
    {
        public Task<BlogAiFixResult> FixHtmlAsync(string html, CancellationToken cancellationToken, AiFixRequestOptions? options = null)
        {
            return callback(html, options);
        }
    }
}
