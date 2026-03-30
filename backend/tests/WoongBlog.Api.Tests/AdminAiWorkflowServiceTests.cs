using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Reflection;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Endpoints;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

public class AdminAiWorkflowServiceTests
{
    [Fact]
    public void RuntimeConfig_ReturnsNormalizedProvider()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, new FakeBlogAiFixService(), new AiOptions { Provider = "azure-openai", AzureOpenAiDeployment = "dep", CodexModel = "gpt-5.4", CodexReasoningEffort = "medium", OpenAiModel = "gpt-4.1" });

        var result = service.RuntimeConfig();

        Assert.Equal("azure", result.Provider);
        Assert.Equal("dep", result.DefaultModel);
    }

    [Theory]
    [InlineData("codex", "codex")]
    [InlineData("azureopenai", "azure")]
    [InlineData("openai", "openai")]
    public void RuntimeConfig_Normalizes_All_Provider_Shapes(string provider, string expected)
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, new FakeBlogAiFixService(), new AiOptions { Provider = provider, AzureOpenAiDeployment = "dep", CodexModel = "gpt-5.4", CodexReasoningEffort = "medium", OpenAiModel = "gpt-4.1" });

        var result = service.RuntimeConfig();

        Assert.Equal(expected, result.Provider);
    }

    [Fact]
    public async Task FixBlogAsync_ReturnsValidationFailure_WhenHtmlMissing()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.FixBlogAsync(new AdminAiEndpoints.BlogFixRequest(""), CancellationToken.None);

        Assert.NotNull(result.Failure);
        Assert.Equal(AdminAiFailureKind.Validation, result.Failure!.Kind);
    }

    [Fact]
    public async Task FixBlogAsync_ReturnsFixedHtml_WhenValid()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, new FakeBlogAiFixService("<p>fixed</p>"));

        var result = await service.FixBlogAsync(new AdminAiEndpoints.BlogFixRequest("<p>hello</p>", "gpt-5.4", "medium"), CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.Equal("<p>fixed</p>", result.Value!.FixedHtml);
    }

    [Fact]
    public async Task FixBlogBatchAsync_ReturnsValidationFailure_WhenSelectionMissing()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.FixBlogBatchAsync(new AdminAiEndpoints.BlogFixBatchRequest(null, false, false), CancellationToken.None);

        Assert.Equal(AdminAiFailureKind.Validation, result.Failure!.Kind);
    }

    [Fact]
    public async Task FixBlogBatchAsync_AppliesHtml_WhenRequested()
    {
        using var dbContext = CreateDbContext();
        var blog = SeedBlog("blog-1");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, new FakeBlogAiFixService("<p>fixed batch</p>"));

        var result = await service.FixBlogBatchAsync(new AdminAiEndpoints.BlogFixBatchRequest([blog.Id], false, true), CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.Contains("fixed batch", dbContext.Blogs.Single().ContentJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnrichWorkAsync_ReturnsValidationFailure_WhenHtmlMissing()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.EnrichWorkAsync(new AdminAiEndpoints.WorkEnrichRequest(""), CancellationToken.None);

        Assert.Equal(AdminAiFailureKind.Validation, result.Failure!.Kind);
    }

    [Fact]
    public async Task EnrichWorkAsync_ReturnsFixedHtml_WhenValid()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, new FakeBlogAiFixService("<p>work fixed</p>"));

        var result = await service.EnrichWorkAsync(new AdminAiEndpoints.WorkEnrichRequest("<p>hello</p>", "Title"), CancellationToken.None);

        Assert.Equal("<p>work fixed</p>", result.Value!.FixedHtml);
    }

    [Fact]
    public void PrivateHelperMethods_Handle_All_Normalization_Branches()
    {
        var type = typeof(AdminAiWorkflowService);
        var normalizeProvider = type.GetMethod("NormalizeProvider", BindingFlags.NonPublic | BindingFlags.Static)!;
        var resolveModel = type.GetMethod("ResolveCodexModel", BindingFlags.NonPublic | BindingFlags.Static)!;
        var resolveReasoning = type.GetMethod("ResolveCodexReasoningEffort", BindingFlags.NonPublic | BindingFlags.Static)!;
        var buildSelectionKey = type.GetMethod("BuildSelectionKey", BindingFlags.NonPublic | BindingFlags.Static)!;
        var normalizeWorkerCount = type.GetMethod("NormalizeWorkerCount", BindingFlags.NonPublic | BindingFlags.Static)!;
        var options = new AiOptions
        {
            CodexModel = "gpt-5.4",
            CodexReasoningEffort = "medium",
            CodexAllowedModels = ["gpt-5.4", "gpt-5.3-codex"],
            CodexAllowedReasoningEfforts = ["low", "medium", "high"]
        };

        Assert.Equal("azure", normalizeProvider.Invoke(null, ["azureopenai"]));
        Assert.Equal("azure", normalizeProvider.Invoke(null, ["azure-openai"]));
        Assert.Equal("azure", normalizeProvider.Invoke(null, ["azure"]));
        Assert.Equal("codex", normalizeProvider.Invoke(null, ["codex"]));
        Assert.Equal("openai", normalizeProvider.Invoke(null, [null]));

        Assert.Equal("gpt-5.3-codex", resolveModel.Invoke(null, [options, "gpt-5.3-codex"]));
        Assert.Equal("gpt-5.4", resolveModel.Invoke(null, [options, "invalid"]));
        Assert.Equal("gpt-5.4", resolveModel.Invoke(null, [options, null]));
        Assert.Equal("medium", resolveReasoning.Invoke(null, [options, "medium"]));
        Assert.Equal("medium", resolveReasoning.Invoke(null, [options, "invalid"]));
        Assert.Equal("medium", resolveReasoning.Invoke(null, [options, null]));
        Assert.Null(normalizeWorkerCount.Invoke(null, [null]));
        Assert.Equal(8, normalizeWorkerCount.Invoke(null, [99]));

        var first = (string)buildSelectionKey.Invoke(null, ["selected", new[] { Guid.Parse("00000000-0000-0000-0000-000000000002"), Guid.Parse("00000000-0000-0000-0000-000000000001") }, "gpt-5.4", "medium", false, false, 2])!;
        var second = (string)buildSelectionKey.Invoke(null, ["selected", new[] { Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Parse("00000000-0000-0000-0000-000000000002") }, "gpt-5.4", "medium", false, false, 2])!;
        var third = (string)buildSelectionKey.Invoke(null, ["range", new[] { Guid.Parse("00000000-0000-0000-0000-000000000001") }, "gpt-5.4", null, true, true, null])!;

        Assert.Equal(first, second);
        Assert.StartsWith("sha256:", first);
        Assert.StartsWith("sha256:", third);
    }

    [Fact]
    public async Task CreateBlogFixBatchJobAsync_CreatesJob_AndListGetApplyFlow_Works()
    {
        using var dbContext = CreateDbContext();
        var blogA = SeedBlog("blog-a");
        var blogB = SeedBlog("blog-b");
        dbContext.Blogs.AddRange(blogA, blogB);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, new FakeBlogAiFixService("<p>fixed html</p>"));

        var created = await service.CreateBlogFixBatchJobAsync(new AdminAiEndpoints.BlogFixBatchJobCreateRequest([blogA.Id, blogB.Id], false, AutoApply: false), CancellationToken.None);
        var jobId = created.Value!.JobId;

        Assert.Null(created.Failure);

        var listed = await service.ListBlogFixBatchJobsAsync(CancellationToken.None);
        Assert.Contains(listed.Value!.Jobs, job => job.JobId == jobId);

        var detail = await service.GetBlogFixBatchJobAsync(jobId, CancellationToken.None);
        Assert.Equal(jobId, detail.Value!.JobId);

        var items = dbContext.AiBatchJobItems.Where(x => x.JobId == jobId).ToList();
        items[0].RecordSuccess("<p>fixed html</p>", "fake", "fake-model", null);
        items[1].RecordSuccess("<p>fixed html</p>", "fake", "fake-model", null);
        await dbContext.SaveChangesAsync();

        var applied = await service.ApplyBlogFixBatchJobAsync(jobId, new AdminAiEndpoints.BlogFixBatchJobApplyRequest(), CancellationToken.None);

        Assert.All(applied.Value!.Items, item => Assert.Equal("applied", item.Status));
    }

    [Fact]
    public async Task CreateBlogFixBatchJobAsync_ReusesExistingQueuedJob_AndFallsBackInvalidCodexOverrides()
    {
        using var dbContext = CreateDbContext();
        var blog = SeedBlog("blog-reuse");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new FakeBlogAiFixService(), new AiOptions
        {
            Provider = "codex",
            CodexModel = "gpt-5.4",
            CodexReasoningEffort = "medium",
            CodexAllowedModels = ["gpt-5.4"],
            CodexAllowedReasoningEfforts = ["medium"]
        });

        var first = await service.CreateBlogFixBatchJobAsync(
            new AdminAiEndpoints.BlogFixBatchJobCreateRequest([blog.Id], false, SelectionKey: "sha256:reuse", CodexModel: "bad-model", CodexReasoningEffort: "bad-effort"),
            CancellationToken.None);
        var result = await service.CreateBlogFixBatchJobAsync(
            new AdminAiEndpoints.BlogFixBatchJobCreateRequest([blog.Id], false, SelectionKey: "sha256:reuse", CodexModel: "bad-model", CodexReasoningEffort: "bad-effort"),
            CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.Equal(first.Value!.JobId, result.Value!.JobId);
    }

    [Fact]
    public async Task CreateBlogFixBatchJobAsync_ReturnsValidation_WhenAllSelectionHasNoBlogs()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.CreateBlogFixBatchJobAsync(
            new AdminAiEndpoints.BlogFixBatchJobCreateRequest(null, true),
            CancellationToken.None);

        Assert.Equal(AdminAiFailureKind.Validation, result.Failure!.Kind);
    }

    [Fact]
    public async Task CreateBlogFixBatchJobAsync_ReturnsValidation_WhenIdsMissing_ForSelectedMode()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.CreateBlogFixBatchJobAsync(
            new AdminAiEndpoints.BlogFixBatchJobCreateRequest(null, false),
            CancellationToken.None);

        Assert.Equal(AdminAiFailureKind.Validation, result.Failure!.Kind);
    }

    [Fact]
    public async Task ListBlogFixBatchJobsAsync_Returns_All_Status_Counts()
    {
        using var dbContext = CreateDbContext();
        var queued = AiBatchJob.CreateBlogFixJob("selected", "queued", "q", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        var running = AiBatchJob.CreateBlogFixJob("selected", "running", "r", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        running.Start(DateTimeOffset.UtcNow);
        var completed = AiBatchJob.CreateBlogFixJob("selected", "completed", "c", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        completed.Finish(1, 1, 1, 0, DateTimeOffset.UtcNow);
        var failed = AiBatchJob.CreateBlogFixJob("selected", "failed", "f", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        failed.Finish(1, 1, 0, 1, DateTimeOffset.UtcNow);
        var cancelled = AiBatchJob.CreateBlogFixJob("selected", "cancelled", "x", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        cancelled.Cancel(DateTimeOffset.UtcNow);
        dbContext.AiBatchJobs.AddRange(queued, running, completed, failed, cancelled);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.ListBlogFixBatchJobsAsync(CancellationToken.None);

        Assert.Equal(1, result.Value!.QueuedCount);
        Assert.Equal(1, result.Value.RunningCount);
        Assert.Equal(1, result.Value.CompletedCount);
        Assert.Equal(1, result.Value.FailedCount);
        Assert.Equal(1, result.Value.CancelledCount);
    }

    [Fact]
    public async Task CreateBlogFixBatchJobAsync_AllSelection_UsesNonCodexRuntimeBranch()
    {
        using var dbContext = CreateDbContext();
        dbContext.Blogs.AddRange(SeedBlog("blog-1"), SeedBlog("blog-2"));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, new FakeBlogAiFixService(), new AiOptions
        {
            Provider = "openai",
            OpenAiModel = "gpt-4.1"
        });

        var result = await service.CreateBlogFixBatchJobAsync(
            new AdminAiEndpoints.BlogFixBatchJobCreateRequest(null, true, AutoApply: true),
            CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.Equal("gpt-4.1", result.Value!.Model);
        Assert.True(result.Value.AutoApply);
    }

    [Fact]
    public async Task CreateBlogFixBatchJobAsync_UsesAzureRuntimeBranch()
    {
        using var dbContext = CreateDbContext();
        dbContext.Blogs.Add(SeedBlog("blog-azure"));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, new FakeBlogAiFixService(), new AiOptions
        {
            Provider = "azure",
            AzureOpenAiDeployment = "azure-deployment"
        });

        var result = await service.CreateBlogFixBatchJobAsync(
            new AdminAiEndpoints.BlogFixBatchJobCreateRequest(null, true),
            CancellationToken.None);

        Assert.Equal("azure-deployment", result.Value!.Model);
    }

    [Fact]
    public async Task ApplyBlogFixBatchJobAsync_Honors_Item_Filter_And_Skips_NonApplicable_Items()
    {
        using var dbContext = CreateDbContext();
        var blogA = SeedBlog("blog-a");
        var blogB = SeedBlog("blog-b");
        dbContext.Blogs.AddRange(blogA, blogB);
        var job = AiBatchJob.CreateBlogFixJob("selected", "2 selected", "filter", false, false, 1, 2, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        var itemA = AiBatchJobItem.Create(job.Id, blogA.Id, blogA.Title, DateTimeOffset.UtcNow);
        itemA.RecordSuccess("<p>fixed-a</p>", "fake", "fake", null);
        var itemB = AiBatchJobItem.Create(job.Id, blogB.Id, blogB.Title, DateTimeOffset.UtcNow);
        itemB.RecordSuccess(string.Empty, "fake", "fake", null);
        dbContext.AiBatchJobs.Add(job);
        dbContext.AiBatchJobItems.AddRange(itemA, itemB);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ApplyBlogFixBatchJobAsync(job.Id, new AdminAiEndpoints.BlogFixBatchJobApplyRequest([itemA.Id, itemB.Id]), CancellationToken.None);

        Assert.Equal("applied", result.Value!.Items.Single(x => x.JobItemId == itemA.Id).Status);
        Assert.Equal("succeeded", result.Value.Items.Single(x => x.JobItemId == itemB.Id).Status);
    }

    [Fact]
    public async Task ApplyBlogFixBatchJobAsync_ReturnsNotFound_WhenJobMissing()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.ApplyBlogFixBatchJobAsync(Guid.NewGuid(), new AdminAiEndpoints.BlogFixBatchJobApplyRequest(), CancellationToken.None);

        Assert.Equal(AdminAiFailureKind.NotFound, result.Failure!.Kind);
    }

    [Fact]
    public async Task ApplyBlogFixBatchJobAsync_Skips_Items_When_BlogMissing()
    {
        using var dbContext = CreateDbContext();
        var job = AiBatchJob.CreateBlogFixJob("selected", "missing-blog", "missing-blog", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        var item = AiBatchJobItem.Create(job.Id, Guid.NewGuid(), "Missing", DateTimeOffset.UtcNow);
        item.RecordSuccess("<p>fixed</p>", "fake", "fake", null);
        dbContext.AiBatchJobs.Add(job);
        dbContext.AiBatchJobItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ApplyBlogFixBatchJobAsync(job.Id, new AdminAiEndpoints.BlogFixBatchJobApplyRequest(), CancellationToken.None);

        Assert.Equal("succeeded", result.Value!.Items.Single().Status);
    }

    [Fact]
    public async Task CancelBlogFixBatchJobAsync_ReturnsTerminalJob_Unchanged()
    {
        using var dbContext = CreateDbContext();
        var job = AiBatchJob.CreateBlogFixJob("selected", "done", "terminal", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        job.Finish(1, 1, 1, 0, DateTimeOffset.UtcNow);
        dbContext.AiBatchJobs.Add(job);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CancelBlogFixBatchJobAsync(job.Id, CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.Equal("completed", result.Value!.Status);
    }

    [Fact]
    public async Task CancelQueuedBlogFixBatchJobsAsync_LeavesNonPendingItemsUntouched()
    {
        using var dbContext = CreateDbContext();
        var blog = SeedBlog("blog-queued");
        dbContext.Blogs.Add(blog);
        var queued = AiBatchJob.CreateBlogFixJob("selected", "queued", "queued", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        var item = AiBatchJobItem.Create(queued.Id, blog.Id, blog.Title, DateTimeOffset.UtcNow);
        item.Start(DateTimeOffset.UtcNow);
        dbContext.AiBatchJobs.Add(queued);
        dbContext.AiBatchJobItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CancelQueuedBlogFixBatchJobsAsync(CancellationToken.None);

        Assert.Equal(1, result.Value!.Count);
        Assert.Equal("running", dbContext.AiBatchJobItems.Single().Status);
    }

    [Fact]
    public async Task CancelQueuedBlogFixBatchJobsAsync_ReturnsZero_WhenNoQueuedJobs()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.CancelQueuedBlogFixBatchJobsAsync(CancellationToken.None);

        Assert.Equal(0, result.Value!.Count);
    }

    [Fact]
    public async Task ClearCompletedBlogFixBatchJobsAsync_Removes_Completed_Jobs_And_Items()
    {
        using var dbContext = CreateDbContext();
        var blog = SeedBlog("blog-clear");
        dbContext.Blogs.Add(blog);
        var completed = AiBatchJob.CreateBlogFixJob("selected", "completed", "clear", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        completed.Finish(1, 1, 1, 0, DateTimeOffset.UtcNow);
        var item = AiBatchJobItem.Create(completed.Id, blog.Id, blog.Title, DateTimeOffset.UtcNow);
        dbContext.AiBatchJobs.Add(completed);
        dbContext.AiBatchJobItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ClearCompletedBlogFixBatchJobsAsync(CancellationToken.None);

        Assert.Equal(1, result.Value!.Count);
        Assert.Empty(dbContext.AiBatchJobs);
        Assert.Empty(dbContext.AiBatchJobItems);
    }

    [Fact]
    public async Task RefreshJobAggregatesAsync_ComputesMixedStatusCounts()
    {
        using var dbContext = CreateDbContext();
        var blog = SeedBlog("blog-refresh");
        dbContext.Blogs.Add(blog);
        var job = AiBatchJob.CreateBlogFixJob("selected", "mix", "mix", false, false, 1, 5, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        var pending = AiBatchJobItem.Create(job.Id, blog.Id, "Pending", DateTimeOffset.UtcNow);
        var succeeded = AiBatchJobItem.Create(job.Id, blog.Id, "Succeeded", DateTimeOffset.UtcNow);
        succeeded.RecordSuccess("<p>ok</p>", "fake", "fake", null);
        var failed = AiBatchJobItem.Create(job.Id, blog.Id, "Failed", DateTimeOffset.UtcNow);
        failed.RecordFailure("boom");
        var applied = AiBatchJobItem.Create(job.Id, blog.Id, "Applied", DateTimeOffset.UtcNow);
        applied.RecordSuccess("<p>ok</p>", "fake", "fake", null);
        applied.MarkApplied(DateTimeOffset.UtcNow);
        var cancelled = AiBatchJobItem.Create(job.Id, blog.Id, "Cancelled", DateTimeOffset.UtcNow);
        cancelled.Cancel(DateTimeOffset.UtcNow);
        dbContext.AiBatchJobs.Add(job);
        dbContext.AiBatchJobItems.AddRange(pending, succeeded, failed, applied, cancelled);
        await dbContext.SaveChangesAsync();

        var refresh = typeof(AdminAiWorkflowService).GetMethod("RefreshJobAggregatesAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        await (Task)refresh.Invoke(null, [dbContext, job, CancellationToken.None])!;

        Assert.Equal(5, job.TotalCount);
        Assert.Equal(4, job.ProcessedCount);
        Assert.Equal(2, job.SucceededCount);
        Assert.Equal(1, job.FailedCount);
    }

    [Fact]
    public async Task FixBlogBatchAsync_Records_Failed_Items_When_AiThrows()
    {
        using var dbContext = CreateDbContext();
        var blog = SeedBlog("blog-failure");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, new ThrowingBlogAiFixService());

        var result = await service.FixBlogBatchAsync(new AdminAiEndpoints.BlogFixBatchRequest([blog.Id], false, false), CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.Equal("failed", result.Value!.Results.Single().Status);
    }

    [Fact]
    public async Task RemoveBlogFixBatchJobAsync_ReturnsNotFound_WhenMissing()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.RemoveBlogFixBatchJobAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(AdminAiFailureKind.NotFound, result.Failure!.Kind);
    }

    [Fact]
    public async Task CancelAndCleanupOperations_Handle_NotFound_Conflict_AndTerminalStates()
    {
        using var dbContext = CreateDbContext();
        var blog = SeedBlog("blog-c");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var notFoundCancel = await service.CancelBlogFixBatchJobAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Equal(AdminAiFailureKind.NotFound, notFoundCancel.Failure!.Kind);

        var created = await service.CreateBlogFixBatchJobAsync(new AdminAiEndpoints.BlogFixBatchJobCreateRequest([blog.Id], false, AutoApply: false), CancellationToken.None);
        var jobId = created.Value!.JobId;

        var cancel = await service.CancelBlogFixBatchJobAsync(jobId, CancellationToken.None);
        Assert.Equal(jobId, cancel.Value!.JobId);

        var cancelQueued = await service.CancelQueuedBlogFixBatchJobsAsync(CancellationToken.None);
        Assert.NotNull(cancelQueued.Value);

        var clear = await service.ClearCompletedBlogFixBatchJobsAsync(CancellationToken.None);
        Assert.NotNull(clear.Value);

        var runningJob = AiBatchJob.CreateBlogFixJob("selected", "running", "running", false, false, 1, 1, "codex", "gpt-5.4", "medium", DateTimeOffset.UtcNow);
        runningJob.Start(DateTimeOffset.UtcNow);
        dbContext.AiBatchJobs.Add(runningJob);
        await dbContext.SaveChangesAsync();

        var removeConflict = await service.RemoveBlogFixBatchJobAsync(runningJob.Id, CancellationToken.None);
        Assert.Equal(AdminAiFailureKind.Conflict, removeConflict.Failure!.Kind);

        var removed = await service.RemoveBlogFixBatchJobAsync(jobId, CancellationToken.None);
        Assert.Equal(1, removed.Value!.Removed);
    }

    private static WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    private static AdminAiWorkflowService CreateService(WoongBlogDbContext dbContext, IBlogAiFixService? aiFixService = null, AiOptions? options = null)
        => new(dbContext, aiFixService ?? new FakeBlogAiFixService(), Options.Create(options ?? new AiOptions()));

    private static Blog SeedBlog(string slug)
        => Blog.Seed(new BlogUpsertValues($"Title {slug}", ["tag"], true, """{"html":"<p>Body</p>"}""", null), slug, "excerpt", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), DateTimeOffset.UtcNow);

    private sealed class FakeBlogAiFixService : IBlogAiFixService
    {
        private readonly string _fixedHtml;

        public FakeBlogAiFixService(string fixedHtml = "<p>fixed</p>")
        {
            _fixedHtml = fixedHtml;
        }

        public Task<BlogAiFixResult> FixHtmlAsync(string html, CancellationToken cancellationToken, AiFixRequestOptions? options = null)
            => Task.FromResult(new BlogAiFixResult(_fixedHtml, "fake", options?.CodexModel ?? "fake-model", options?.CodexReasoningEffort));
    }

    private sealed class ThrowingBlogAiFixService : IBlogAiFixService
    {
        public Task<BlogAiFixResult> FixHtmlAsync(string html, CancellationToken cancellationToken, AiFixRequestOptions? options = null)
            => throw new InvalidOperationException("forced failure");
    }
}
