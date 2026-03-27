using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Portfolio.Api.Infrastructure.Ai;
using Portfolio.Api.Infrastructure.Persistence;

namespace Portfolio.Api.Tests;

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
        var dbContext = seedScope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
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
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
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
        Assert.Contains("codexModel", payload, StringComparison.OrdinalIgnoreCase);
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

    private sealed class FakeBlogAiFixService : IBlogAiFixService
    {
        public Task<BlogAiFixResult> FixHtmlAsync(string html, CancellationToken cancellationToken, AiFixRequestOptions? options = null)
        {
            return Task.FromResult(new BlogAiFixResult("<p>fixed-html</p>", "fake", options?.CodexModel ?? "fake-model", options?.CodexReasoningEffort));
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
}
