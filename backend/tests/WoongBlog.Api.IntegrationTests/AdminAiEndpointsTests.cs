using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Integration)]
public class AdminAiEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminAiEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FixBlog_ReturnsBadRequest_WhenHtmlMissing()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/admin/ai/blog-fix", new { html = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FixBlog_ReturnsProviderPayload()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/admin/ai/blog-fix", new
        {
            html = "<p>Hello</p>",
            codexModel = "gpt-5.4",
            codexReasoningEffort = "medium"
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("fixed-html", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fake", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FixBlog_ForwardsRequestedProvider()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/admin/ai/blog-fix", new
        {
            html = "<p>Hello</p>",
            provider = "openai"
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("openai", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FixBlog_ForwardsCustomPrompt()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/admin/ai/blog-fix", new
        {
            html = "<p>Hello</p>",
            customPrompt = "Use terse editorial fixes only."
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Use terse editorial fixes only.", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FixBlogBatch_AppliesUpdatedHtml_WhenRequested()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        using var seedScope = factory.Services.CreateScope();
        var dbContext = seedScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var blogId = dbContext.Blogs.OrderBy(x => x.CreatedAt).Select(x => x.Id).First();

        var response = await client.PostAsJsonAsync("/api/admin/ai/blog-fix-batch", new
        {
            blogIds = new[] { blogId },
            all = false,
            apply = true,
            codexModel = "gpt-5.4",
            codexReasoningEffort = "medium"
        });

        response.EnsureSuccessStatusCode();

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var updatedBlog = verifyDbContext.Blogs.Single(x => x.Id == blogId);
        Assert.Contains("fixed-html", updatedBlog.ContentJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fixed-html", updatedBlog.Excerpt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenApiDocument_ListsAdminAiEndpoints()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/openapi/v1.json");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/admin/ai/blog-fix", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/admin/ai/blog-fix-batch", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RuntimeConfig_ReturnsConfiguredProviderMetadata()
    {
        var client = CreateAuthenticatedClient(_factory);

        var response = await client.GetAsync("/api/admin/ai/runtime-config");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("provider", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("availableProviders", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("codexModel", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("batchConcurrency", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("defaultSystemPrompt", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorkEnrich_ReturnsProviderPayload()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync("/api/admin/ai/work-enrich", new
        {
            html = "<p>Hello</p>",
            title = "Sample Work",
            codexModel = "gpt-5.4",
            codexReasoningEffort = "medium"
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("fixed-html", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fake", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBatchJob_ListsAndReturnsDetail()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var blogIds = dbContext.Blogs.OrderBy(x => x.CreatedAt).Take(2).Select(x => x.Id).ToArray();

        var create = await client.PostAsJsonAsync("/api/admin/ai/blog-fix-batch-jobs", new
        {
            blogIds,
            all = false,
            codexModel = "gpt-5.4",
            codexReasoningEffort = "medium"
        });

        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<BatchJobDetailPayload>();
        Assert.NotNull(created);
        Assert.Equal("queued", created!.Status);
        Assert.Equal(2, created.TotalCount);
        Assert.Contains(created.Provider, new[] { "openai", "codex" });

        var list = await client.GetFromJsonAsync<BatchJobListPayload>("/api/admin/ai/blog-fix-batch-jobs");
        Assert.NotNull(list);
        Assert.Contains(list!.Jobs, job => job.JobId == created.JobId);

        var detail = await client.GetFromJsonAsync<BatchJobDetailPayload>($"/api/admin/ai/blog-fix-batch-jobs/{created.JobId}");
        Assert.NotNull(detail);
        Assert.Equal(created.JobId, detail!.JobId);
        Assert.Equal(2, detail.Items.Count);
    }

    [Fact]
    public async Task BatchJob_PersistsAndAppliesCustomPrompt()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        Guid blogId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            blogId = dbContext.Blogs.OrderBy(x => x.CreatedAt).Select(x => x.Id).First();
        }

        var create = await client.PostAsJsonAsync("/api/admin/ai/blog-fix-batch-jobs", new
        {
            blogIds = new[] { blogId },
            all = false,
            workerCount = 1,
            codexModel = "gpt-5.4",
            codexReasoningEffort = "medium",
            customPrompt = "Preserve terse custom prompt."
        });

        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<BatchJobDetailPayload>();
        Assert.NotNull(created);
        Assert.Equal("Preserve terse custom prompt.", created!.CustomPrompt);

        using (var persistScope = factory.Services.CreateScope())
        {
            var dbContext = persistScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            var persisted = await dbContext.AiBatchJobs.SingleAsync(job => job.Id == created.JobId);
            Assert.Equal("Preserve terse custom prompt.", persisted.CustomPrompt);
        }

        BatchJobDetailPayload? job = null;
        for (var attempt = 0; attempt < 30; attempt += 1)
        {
            job = await client.GetFromJsonAsync<BatchJobDetailPayload>($"/api/admin/ai/blog-fix-batch-jobs/{created.JobId}");
            if (job is not null && (job.Status == "completed" || job.Status == "failed" || job.Status == "cancelled"))
            {
                break;
            }

            await Task.Delay(250);
        }

        Assert.NotNull(job);
        Assert.Equal("completed", job!.Status);
        var item = Assert.Single(job.Items);
        Assert.Equal(created.Provider, item.Provider);
        Assert.Contains("Preserve terse custom prompt.", item.FixedHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BatchJob_CanBeCancelled()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var blogId = dbContext.Blogs.OrderBy(x => x.CreatedAt).Select(x => x.Id).First();

        var create = await client.PostAsJsonAsync("/api/admin/ai/blog-fix-batch-jobs", new
        {
            blogIds = new[] { blogId },
            all = false,
            codexModel = "gpt-5.4",
            codexReasoningEffort = "medium"
        });

        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<BatchJobDetailPayload>();
        Assert.NotNull(created);

        var cancel = await client.PostAsync($"/api/admin/ai/blog-fix-batch-jobs/{created!.JobId}/cancel", JsonContent.Create(new { }));
        cancel.EnsureSuccessStatusCode();
        var payload = await cancel.Content.ReadAsStringAsync();
        Assert.Contains("cancelRequested", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompletedBatchJob_CanApplySuccessfulResults()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        Guid[] blogIds;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            blogIds = dbContext.Blogs.OrderBy(x => x.CreatedAt).Take(2).Select(x => x.Id).ToArray();
        }

        var create = await client.PostAsJsonAsync("/api/admin/ai/blog-fix-batch-jobs", new
        {
            blogIds,
            all = false,
            codexModel = "gpt-5.4",
            codexReasoningEffort = "medium"
        });

        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<BatchJobDetailPayload>();
        Assert.NotNull(created);

        BatchJobDetailPayload? job = null;
        for (var attempt = 0; attempt < 30; attempt += 1)
        {
            job = await client.GetFromJsonAsync<BatchJobDetailPayload>($"/api/admin/ai/blog-fix-batch-jobs/{created!.JobId}");
            if (job is not null && (job.Status == "completed" || job.Status == "failed" || job.Status == "cancelled"))
            {
                break;
            }

            await Task.Delay(250);
        }

        Assert.NotNull(job);
        Assert.Equal("completed", job!.Status);
        Assert.Equal(2, job.SucceededCount);

        var apply = await client.PostAsync($"/api/admin/ai/blog-fix-batch-jobs/{created!.JobId}/apply", JsonContent.Create(new { }));
        apply.EnsureSuccessStatusCode();
        var appliedJob = await apply.Content.ReadFromJsonAsync<BatchJobDetailPayload>();
        Assert.NotNull(appliedJob);
        Assert.All(appliedJob!.Items, item => Assert.Equal("applied", item.Status));

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        foreach (var blogId in blogIds)
        {
            var updatedBlog = await verifyDb.Blogs.SingleAsync(x => x.Id == blogId);
            Assert.Contains("fixed-html", updatedBlog.ContentJson, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CompletedBatchJob_CanBeRemoved()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        Guid[] blogIds;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            blogIds = dbContext.Blogs.OrderBy(x => x.CreatedAt).Take(1).Select(x => x.Id).ToArray();
        }

        var create = await client.PostAsJsonAsync("/api/admin/ai/blog-fix-batch-jobs", new
        {
            blogIds,
            all = false,
            codexModel = "gpt-5.4",
            codexReasoningEffort = "medium"
        });

        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<BatchJobDetailPayload>();
        Assert.NotNull(created);

        BatchJobDetailPayload? job = null;
        for (var attempt = 0; attempt < 30; attempt += 1)
        {
            job = await client.GetFromJsonAsync<BatchJobDetailPayload>($"/api/admin/ai/blog-fix-batch-jobs/{created!.JobId}");
            if (job is not null && job.Status == "completed")
            {
                break;
            }

            await Task.Delay(250);
        }

        Assert.NotNull(job);
        Assert.Equal("completed", job!.Status);

        var remove = await client.DeleteAsync($"/api/admin/ai/blog-fix-batch-jobs/{created!.JobId}");
        remove.EnsureSuccessStatusCode();

        var detail = await client.GetAsync($"/api/admin/ai/blog-fix-batch-jobs/{created!.JobId}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact]
    public async Task CompletedBatchJob_CanAutoApplySuccessfulResults()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlogAiFixService>();
                services.AddScoped<IBlogAiFixService, FakeBlogAiFixService>();
            });
        });

        var client = CreateAuthenticatedClient(factory);

        Guid[] blogIds;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            blogIds = dbContext.Blogs.OrderBy(x => x.CreatedAt).Take(2).Select(x => x.Id).ToArray();
        }

        var create = await client.PostAsJsonAsync("/api/admin/ai/blog-fix-batch-jobs", new
        {
            blogIds,
            all = false,
            autoApply = true,
            workerCount = 1,
            codexModel = "gpt-5.4",
            codexReasoningEffort = "medium"
        });

        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<BatchJobDetailPayload>();
        Assert.NotNull(created);

        BatchJobDetailPayload? job = null;
        for (var attempt = 0; attempt < 30; attempt += 1)
        {
            job = await client.GetFromJsonAsync<BatchJobDetailPayload>($"/api/admin/ai/blog-fix-batch-jobs/{created!.JobId}");
            if (job is not null && job.Status == "completed")
            {
                break;
            }

            await Task.Delay(250);
        }

        Assert.NotNull(job);
        Assert.True(job!.AutoApply);
        Assert.Equal(1, job.WorkerCount);
        Assert.All(job.Items, item => Assert.Equal("applied", item.Status));

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        foreach (var blogId in blogIds)
        {
            var updatedBlog = await verifyDb.Blogs.SingleAsync(x => x.Id == blogId);
            Assert.Contains("fixed-html", updatedBlog.ContentJson, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class FakeBlogAiFixService : IBlogAiFixService
    {
        public Task<BlogAiFixResult> FixHtmlAsync(string html, CancellationToken cancellationToken, AiFixRequestOptions? options = null)
        {
            var fixedHtml = string.IsNullOrWhiteSpace(options?.CustomPrompt)
                ? "<p>fixed-html</p>"
                : $"<p>fixed-html</p><p>{options.CustomPrompt}</p>";
            return Task.FromResult(new BlogAiFixResult(fixedHtml, options?.Provider ?? "fake", options?.CodexModel ?? "fake-model", options?.CodexReasoningEffort));
        }
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "admin");

        var csrfPayload = client.GetFromJsonAsync<CsrfTokenResponse>("/api/auth/csrf").GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(csrfPayload?.RequestToken))
        {
            client.DefaultRequestHeaders.Add(csrfPayload.HeaderName, csrfPayload.RequestToken);
        }

        return client;
    }

    private sealed record CsrfTokenResponse(string RequestToken, string HeaderName);
    private sealed record BatchJobListPayload(
        IReadOnlyList<BatchJobSummaryPayload> Jobs,
        int RunningCount = 0,
        int QueuedCount = 0,
        int CompletedCount = 0,
        int FailedCount = 0,
        int CancelledCount = 0);
    private sealed record BatchJobSummaryPayload(Guid JobId, string Status, int TotalCount, int ProcessedCount, int SucceededCount, int FailedCount);
    private sealed record BatchJobDetailPayload(
        Guid JobId,
        string Status,
        string Provider,
        string? CustomPrompt,
        bool AutoApply,
        int? WorkerCount,
        int TotalCount,
        int ProcessedCount,
        int SucceededCount,
        int FailedCount,
        IReadOnlyList<BatchJobItemPayload> Items);
    private sealed record BatchJobItemPayload(Guid JobItemId, Guid BlogId, string Title, string Status, string? FixedHtml, string? Error, string? Provider);
}
