using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

public class AdminEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateWork_WithInvalidPayload_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title = "",
            category = "",
            contentJson = "",
            tags = new[] { new string('x', 51) }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWork_ThenGetById_PersistsExcerptAndCategory()
    {
        var client = _factory.CreateAuthenticatedClient();
        var createResponse = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title = "Integration Work",
            category = "integration",
            period = "2026.03 - 2026.03",
            contentJson = "{\"html\":\"<p>Integration work body</p>\"}",
            allPropertiesJson = "{\"priority\":\"high\"}",
            tags = new[] { "integration", "tests" },
            published = true
        });

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var id = created!["id"];

        var getResponse = await client.GetAsync($"/api/admin/works/{id}");
        getResponse.EnsureSuccessStatusCode();
        var body = await getResponse.Content.ReadAsStringAsync();

        Assert.Contains("Integration Work", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("integration", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Integration work body", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdatePage_PersistsNewHtmlContent()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var introductionPage = dbContext.Pages.Single(page => page.Slug == "introduction");

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync("/api/admin/pages", new
        {
            id = introductionPage.Id,
            title = "Introduction",
            contentJson = "{\"html\":\"<p>Updated from integration test</p>\"}"
        });

        response.EnsureSuccessStatusCode();

        var publicResponse = await client.GetAsync("/api/public/pages/introduction");
        publicResponse.EnsureSuccessStatusCode();
        var body = await publicResponse.Content.ReadAsStringAsync();

        Assert.Contains("Updated from integration test", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateSiteSettings_PersistsOwnerName()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName = "Integration Owner",
            tagline = "Integration Tagline"
        });

        response.EnsureSuccessStatusCode();

        var publicResponse = await client.GetAsync("/api/public/site-settings");
        publicResponse.EnsureSuccessStatusCode();
        var body = await publicResponse.Content.ReadAsStringAsync();

        Assert.Contains("Integration Owner", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Integration Tagline", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBlog_WithTooLongTitle_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = new string('b', 201),
            contentJson = "{\"html\":\"<p>Body</p>\"}",
            tags = new[] { "ok" },
            published = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBlog_ThenDelete_RemovesBlog()
    {
        var client = _factory.CreateAuthenticatedClient();
        var createResponse = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "Integration Blog",
            contentJson = "{\"html\":\"<p>Blog body</p>\"}",
            tags = new[] { "integration" },
            published = true
        });

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var id = created!["id"];

        var deleteResponse = await client.DeleteAsync($"/api/admin/blogs/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/admin/blogs/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAdminBlogById_ExtractsHtmlContent()
    {
        var client = _factory.CreateAuthenticatedClient();
        var createResponse = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "Extract Html Blog",
            contentJson = "{\"html\":\"<p>Extract me</p>\"}",
            tags = new[] { "integration" },
            published = true
        });

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        var getResponse = await client.GetAsync($"/api/admin/blogs/{created!["id"]}");
        getResponse.EnsureSuccessStatusCode();
        var body = await getResponse.Content.ReadAsStringAsync();

        Assert.Contains("\"html\":\"<p>Extract me</p>\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAdminWorkById_UsesEmptyHtmlWhenContentJsonIsMalformed()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var work = new WoongBlog.Api.Domain.Entities.Work
        {
            Id = Guid.NewGuid(),
            Title = "Malformed Html Work",
            Slug = "malformed-html-work",
            Excerpt = "broken",
            Category = "integration",
            ContentJson = "{not-json}",
            AllPropertiesJson = "{}",
            Published = true,
            PublishedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        dbContext.Works.Add(work);
        dbContext.SaveChanges();

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/admin/works/{work.Id}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"html\":\"\"", body, StringComparison.OrdinalIgnoreCase);
    }
}
