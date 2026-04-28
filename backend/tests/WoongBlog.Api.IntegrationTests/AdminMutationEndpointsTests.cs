using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Integration)]
public class AdminMutationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminMutationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized, "page-update")]
    [InlineData("user", HttpStatusCode.Forbidden, "page-update")]
    [InlineData(null, HttpStatusCode.Unauthorized, "blog-create")]
    [InlineData("user", HttpStatusCode.Forbidden, "blog-create")]
    [InlineData(null, HttpStatusCode.Unauthorized, "blog-update")]
    [InlineData("user", HttpStatusCode.Forbidden, "blog-update")]
    [InlineData(null, HttpStatusCode.Unauthorized, "blog-delete")]
    [InlineData("user", HttpStatusCode.Forbidden, "blog-delete")]
    [InlineData(null, HttpStatusCode.Unauthorized, "work-create")]
    [InlineData("user", HttpStatusCode.Forbidden, "work-create")]
    [InlineData(null, HttpStatusCode.Unauthorized, "work-update")]
    [InlineData("user", HttpStatusCode.Forbidden, "work-update")]
    [InlineData(null, HttpStatusCode.Unauthorized, "work-delete")]
    [InlineData("user", HttpStatusCode.Forbidden, "work-delete")]
    [InlineData(null, HttpStatusCode.Unauthorized, "site-settings-update")]
    [InlineData("user", HttpStatusCode.Forbidden, "site-settings-update")]
    public async Task AdminMutationEndpoints_WithValidCsrf_ButMissingAdminRole_ReturnAuthFailure(
        string? identity,
        HttpStatusCode expectedStatus,
        string mutation)
    {
        var client = await CreateClientWithCsrfAsync(identity);

        var response = await SendMutationAsync(client, mutation);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePage_WhenValid_PersistsTargetPageOnly()
    {
        Guid targetId;
        Guid unrelatedId;
        string targetSlug;
        string unrelatedTitle;
        string unrelatedContentJson;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            var target = dbContext.Pages.Single(page => page.Slug == "introduction");
            var unrelated = dbContext.Pages.Single(page => page.Slug == "contact");
            targetId = target.Id;
            targetSlug = target.Slug;
            unrelatedId = unrelated.Id;
            unrelatedTitle = unrelated.Title;
            unrelatedContentJson = unrelated.ContentJson;
        }

        var client = _factory.CreateAuthenticatedClient();
        var contentJson = JsonSerializer.Serialize(new { html = "<p>Admin mutation page update</p>" });

        var response = await client.PutAsJsonAsync("/api/admin/pages", new
        {
            id = targetId,
            title = "Updated Introduction",
            contentJson
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SuccessPayload>();
        Assert.NotNull(payload);
        Assert.True(payload!.Success);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var updated = await verificationDb.Pages.SingleAsync(page => page.Id == targetId);
        var unrelatedAfter = await verificationDb.Pages.SingleAsync(page => page.Id == unrelatedId);

        Assert.Equal("Updated Introduction", updated.Title);
        Assert.Equal(contentJson, updated.ContentJson);
        Assert.Equal(targetSlug, updated.Slug);
        Assert.Equal(unrelatedTitle, unrelatedAfter.Title);
        Assert.Equal(unrelatedContentJson, unrelatedAfter.ContentJson);
    }

    [Fact]
    public async Task UpdatePage_WhenInvalid_ReturnsBadRequestAndDoesNotPersist()
    {
        Guid pageId;
        string originalTitle;
        string originalContentJson;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            var page = dbContext.Pages.Single(x => x.Slug == "introduction");
            pageId = page.Id;
            originalTitle = page.Title;
            originalContentJson = page.ContentJson;
        }

        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync("/api/admin/pages", new
        {
            id = pageId,
            title = "",
            contentJson = "{\"html\":\"<p>Should not persist</p>\"}"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var pageAfter = await verificationDb.Pages.SingleAsync(x => x.Id == pageId);
        Assert.Equal(originalTitle, pageAfter.Title);
        Assert.Equal(originalContentJson, pageAfter.ContentJson);
    }

    [Fact]
    public async Task CreateBlog_WhenValid_PersistsExpectedFields()
    {
        var unrelatedBlog = await SeedBlogAsync("Unrelated Blog Create", published: true);
        var client = _factory.CreateAuthenticatedClient();
        var contentJson = JsonSerializer.Serialize(new { html = "<p>Created blog body for admin mutation tests</p>" });

        var response = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "Admin Mutation Created Blog",
            excerpt = "Manual create excerpt",
            tags = new[] { "admin", "mutation" },
            published = true,
            contentJson
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreatedPayload>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload!.Id);
        Assert.Equal("admin-mutation-created-blog", payload.Slug);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var created = await dbContext.Blogs.SingleAsync(blog => blog.Id == payload.Id);
        var unrelatedAfter = await dbContext.Blogs.SingleAsync(blog => blog.Id == unrelatedBlog.Id);

        Assert.Equal("Admin Mutation Created Blog", created.Title);
        Assert.Equal("Manual create excerpt", created.Excerpt);
        Assert.Equal(["admin", "mutation"], created.Tags);
        Assert.Equal(contentJson, created.ContentJson);
        Assert.True(created.Published);
        Assert.NotNull(created.PublishedAt);
        Assert.Equal(unrelatedBlog.Title, unrelatedAfter.Title);
        Assert.Equal(unrelatedBlog.ContentJson, unrelatedAfter.ContentJson);
    }

    [Fact]
    public async Task CreateBlog_WhenInvalid_ReturnsBadRequestAndDoesNotCreate()
    {
        var beforeCount = await CountBlogsAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "",
            tags = Array.Empty<string>(),
            published = true,
            contentJson = "{\"html\":\"<p>Invalid blog</p>\"}"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(beforeCount, await CountBlogsAsync());
    }

    [Fact]
    public async Task UpdateBlog_WhenExisting_PersistsExpectedFieldsAndCanUnpublish()
    {
        var target = await SeedBlogAsync("Blog To Update", published: true);
        var unrelated = await SeedBlogAsync("Unrelated Blog Update", published: true);
        var client = _factory.CreateAuthenticatedClient();
        var contentJson = JsonSerializer.Serialize(new { html = "<p>Updated blog body</p>" });

        var response = await client.PutAsJsonAsync($"/api/admin/blogs/{target.Id}", new
        {
            title = "Blog Updated Through Admin Mutation",
            excerpt = "Updated blog excerpt",
            tags = new[] { "updated", "blog" },
            published = false,
            contentJson
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreatedPayload>();
        Assert.NotNull(payload);
        Assert.Equal(target.Id, payload!.Id);
        Assert.Equal("blog-updated-through-admin-mutation", payload.Slug);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var updated = await dbContext.Blogs.SingleAsync(blog => blog.Id == target.Id);
        var unrelatedAfter = await dbContext.Blogs.SingleAsync(blog => blog.Id == unrelated.Id);

        Assert.Equal("Blog Updated Through Admin Mutation", updated.Title);
        Assert.Equal("Updated blog excerpt", updated.Excerpt);
        Assert.Equal(["updated", "blog"], updated.Tags);
        Assert.Equal(contentJson, updated.ContentJson);
        Assert.False(updated.Published);
        Assert.Equal(target.CreatedAt, updated.CreatedAt);
        Assert.Equal(unrelated.Title, unrelatedAfter.Title);
        Assert.Equal(unrelated.ContentJson, unrelatedAfter.ContentJson);
    }

    [Fact]
    public async Task UpdateBlog_WhenMissing_ReturnsNotFoundAndDoesNotAffectExistingBlogs()
    {
        var existing = await SeedBlogAsync("Existing Blog Before Missing Update", published: false);
        var beforeCount = await CountBlogsAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync($"/api/admin/blogs/{Guid.NewGuid()}", new
        {
            title = "Missing Blog Update",
            tags = Array.Empty<string>(),
            published = true,
            contentJson = "{\"html\":\"<p>Missing</p>\"}"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(beforeCount, await CountBlogsAsync());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var existingAfter = await dbContext.Blogs.SingleAsync(blog => blog.Id == existing.Id);
        Assert.Equal(existing.Title, existingAfter.Title);
        Assert.Equal(existing.ContentJson, existingAfter.ContentJson);
    }

    [Fact]
    public async Task DeleteBlog_WhenExisting_RemovesOnlyTargetBlog()
    {
        var target = await SeedBlogAsync("Blog To Delete", published: true);
        var unrelated = await SeedBlogAsync("Unrelated Blog Delete", published: true);
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/admin/blogs/{target.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        Assert.False(await dbContext.Blogs.AnyAsync(blog => blog.Id == target.Id));

        var unrelatedAfter = await dbContext.Blogs.SingleAsync(blog => blog.Id == unrelated.Id);
        Assert.Equal(unrelated.Title, unrelatedAfter.Title);
        Assert.Equal(unrelated.ContentJson, unrelatedAfter.ContentJson);
    }

    [Fact]
    public async Task DeleteBlog_WhenMissing_ReturnsNotFoundAndDoesNotAffectBlogs()
    {
        var existing = await SeedBlogAsync("Existing Blog Before Missing Delete", published: true);
        var beforeCount = await CountBlogsAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/admin/blogs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(beforeCount, await CountBlogsAsync());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        Assert.True(await dbContext.Blogs.AnyAsync(blog => blog.Id == existing.Id));
    }

    [Fact]
    public async Task CreateWork_WhenValid_PersistsExpectedFields()
    {
        var unrelated = await SeedWorkAsync("Unrelated Work Create", published: true);
        var client = _factory.CreateAuthenticatedClient();
        var contentJson = JsonSerializer.Serialize(new { html = "<p>Created work body for admin mutation tests</p>" });
        var propertiesJson = JsonSerializer.Serialize(new { role = "backend", impact = "coverage" });

        var response = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title = "Admin Mutation Created Work",
            category = "platform",
            period = "2026.04",
            tags = new[] { "admin", "work" },
            published = true,
            contentJson,
            allPropertiesJson = propertiesJson
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreatedPayload>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload!.Id);
        Assert.Equal("admin-mutation-created-work", payload.Slug);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var created = await dbContext.Works.SingleAsync(work => work.Id == payload.Id);
        var unrelatedAfter = await dbContext.Works.SingleAsync(work => work.Id == unrelated.Id);

        Assert.Equal("Admin Mutation Created Work", created.Title);
        Assert.Equal("platform", created.Category);
        Assert.Equal("2026.04", created.Period);
        Assert.Equal(["admin", "work"], created.Tags);
        Assert.Equal(contentJson, created.ContentJson);
        Assert.Equal(propertiesJson, created.AllPropertiesJson);
        Assert.True(created.Published);
        Assert.NotNull(created.PublishedAt);
        Assert.Equal(unrelated.Title, unrelatedAfter.Title);
        Assert.Equal(unrelated.ContentJson, unrelatedAfter.ContentJson);
    }

    [Fact]
    public async Task CreateWork_WhenInvalid_ReturnsBadRequestAndDoesNotCreate()
    {
        var beforeCount = await CountWorksAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title = "Invalid Work",
            category = "",
            period = "2026.04",
            tags = Array.Empty<string>(),
            published = true,
            contentJson = "{\"html\":\"<p>Invalid work</p>\"}",
            allPropertiesJson = "{}"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(beforeCount, await CountWorksAsync());
    }

    [Fact]
    public async Task UpdateWork_WhenExisting_PersistsExpectedFieldsAndCanUnpublish()
    {
        var target = await SeedWorkAsync("Work To Update", published: true);
        var unrelated = await SeedWorkAsync("Unrelated Work Update", published: true);
        var client = _factory.CreateAuthenticatedClient();
        var contentJson = JsonSerializer.Serialize(new { html = "<p>Updated work body</p>" });
        var propertiesJson = JsonSerializer.Serialize(new { role = "updated", impact = "admin" });

        var response = await client.PutAsJsonAsync($"/api/admin/works/{target.Id}", new
        {
            title = "Work Updated Through Admin Mutation",
            category = "research",
            period = "2026.05",
            tags = new[] { "updated", "work" },
            published = false,
            contentJson,
            allPropertiesJson = propertiesJson
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreatedPayload>();
        Assert.NotNull(payload);
        Assert.Equal(target.Id, payload!.Id);
        Assert.Equal("work-updated-through-admin-mutation", payload.Slug);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var updated = await dbContext.Works.SingleAsync(work => work.Id == target.Id);
        var unrelatedAfter = await dbContext.Works.SingleAsync(work => work.Id == unrelated.Id);

        Assert.Equal("Work Updated Through Admin Mutation", updated.Title);
        Assert.Equal("research", updated.Category);
        Assert.Equal("2026.05", updated.Period);
        Assert.Equal(["updated", "work"], updated.Tags);
        Assert.Equal(contentJson, updated.ContentJson);
        Assert.Equal(propertiesJson, updated.AllPropertiesJson);
        Assert.False(updated.Published);
        Assert.Equal(target.CreatedAt, updated.CreatedAt);
        Assert.Equal(unrelated.Title, unrelatedAfter.Title);
        Assert.Equal(unrelated.ContentJson, unrelatedAfter.ContentJson);
    }

    [Fact]
    public async Task UpdateWork_WhenMissing_ReturnsNotFoundAndDoesNotAffectExistingWorks()
    {
        var existing = await SeedWorkAsync("Existing Work Before Missing Update", published: false);
        var beforeCount = await CountWorksAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync($"/api/admin/works/{Guid.NewGuid()}", new
        {
            title = "Missing Work Update",
            category = "platform",
            period = "2026.04",
            tags = Array.Empty<string>(),
            published = true,
            contentJson = "{\"html\":\"<p>Missing</p>\"}",
            allPropertiesJson = "{}"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(beforeCount, await CountWorksAsync());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var existingAfter = await dbContext.Works.SingleAsync(work => work.Id == existing.Id);
        Assert.Equal(existing.Title, existingAfter.Title);
        Assert.Equal(existing.ContentJson, existingAfter.ContentJson);
    }

    [Fact]
    public async Task DeleteWork_WhenExisting_RemovesOnlyTargetWork()
    {
        var target = await SeedWorkAsync("Work To Delete", published: true);
        var unrelated = await SeedWorkAsync("Unrelated Work Delete", published: true);
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/admin/works/{target.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        Assert.False(await dbContext.Works.AnyAsync(work => work.Id == target.Id));

        var unrelatedAfter = await dbContext.Works.SingleAsync(work => work.Id == unrelated.Id);
        Assert.Equal(unrelated.Title, unrelatedAfter.Title);
        Assert.Equal(unrelated.ContentJson, unrelatedAfter.ContentJson);
    }

    [Fact]
    public async Task DeleteWork_WhenMissing_ReturnsNotFoundAndDoesNotAffectWorks()
    {
        var existing = await SeedWorkAsync("Existing Work Before Missing Delete", published: true);
        var beforeCount = await CountWorksAsync();
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/admin/works/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(beforeCount, await CountWorksAsync());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        Assert.True(await dbContext.Works.AnyAsync(work => work.Id == existing.Id));
    }

    [Fact]
    public async Task UpdateSiteSettings_WhenPartialPayload_PreservesOmittedFields()
    {
        var original = await SetSiteSettingsAsync(
            ownerName: "Original Owner",
            tagline: "Original Tagline",
            facebookUrl: "https://facebook.example/original",
            instagramUrl: "https://instagram.example/original",
            githubUrl: "https://github.example/original");
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName = "Updated Owner",
            githubUrl = "https://github.example/updated"
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SuccessPayload>();
        Assert.NotNull(payload);
        Assert.True(payload!.Success);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var settings = await dbContext.SiteSettings.SingleAsync(x => x.Singleton);

        Assert.Equal("Updated Owner", settings.OwnerName);
        Assert.Equal("https://github.example/updated", settings.GitHubUrl);
        Assert.Equal(original.Tagline, settings.Tagline);
        Assert.Equal(original.FacebookUrl, settings.FacebookUrl);
        Assert.Equal(original.InstagramUrl, settings.InstagramUrl);
    }

    [Fact]
    public async Task UpdateSiteSettings_WhenInvalid_ReturnsBadRequestAndDoesNotPersist()
    {
        var original = await SetSiteSettingsAsync(
            ownerName: "Owner Before Invalid",
            tagline: "Tagline Before Invalid",
            facebookUrl: "https://facebook.example/before",
            instagramUrl: "https://instagram.example/before",
            githubUrl: "https://github.example/before");
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName = new string('x', 201)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var settings = await dbContext.SiteSettings.SingleAsync(x => x.Singleton);
        Assert.Equal(original.OwnerName, settings.OwnerName);
        Assert.Equal(original.Tagline, settings.Tagline);
        Assert.Equal(original.GitHubUrl, settings.GitHubUrl);
    }

    private async Task<HttpClient> CreateClientWithCsrfAsync(string? identity)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (!string.IsNullOrWhiteSpace(identity))
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, identity);
        }

        var csrfPayload = await client.GetFromJsonAsync<CsrfPayload>("/api/auth/csrf");
        Assert.NotNull(csrfPayload?.RequestToken);
        client.DefaultRequestHeaders.Add(csrfPayload!.HeaderName, csrfPayload.RequestToken);
        return client;
    }

    private static Task<HttpResponseMessage> SendMutationAsync(HttpClient client, string mutation)
    {
        var missingId = Guid.NewGuid();
        return mutation switch
        {
            "page-update" => client.PutAsJsonAsync("/api/admin/pages", new
            {
                id = missingId,
                title = "Auth Check Page",
                contentJson = "{\"html\":\"<p>Auth</p>\"}"
            }),
            "blog-create" => client.PostAsJsonAsync("/api/admin/blogs", new
            {
                title = "Auth Check Blog",
                tags = Array.Empty<string>(),
                published = true,
                contentJson = "{\"html\":\"<p>Auth</p>\"}"
            }),
            "blog-update" => client.PutAsJsonAsync($"/api/admin/blogs/{missingId}", new
            {
                title = "Auth Check Blog Update",
                tags = Array.Empty<string>(),
                published = true,
                contentJson = "{\"html\":\"<p>Auth</p>\"}"
            }),
            "blog-delete" => client.DeleteAsync($"/api/admin/blogs/{missingId}"),
            "work-create" => client.PostAsJsonAsync("/api/admin/works", new
            {
                title = "Auth Check Work",
                category = "platform",
                period = "2026.04",
                tags = Array.Empty<string>(),
                published = true,
                contentJson = "{\"html\":\"<p>Auth</p>\"}",
                allPropertiesJson = "{}"
            }),
            "work-update" => client.PutAsJsonAsync($"/api/admin/works/{missingId}", new
            {
                title = "Auth Check Work Update",
                category = "platform",
                period = "2026.04",
                tags = Array.Empty<string>(),
                published = true,
                contentJson = "{\"html\":\"<p>Auth</p>\"}",
                allPropertiesJson = "{}"
            }),
            "work-delete" => client.DeleteAsync($"/api/admin/works/{missingId}"),
            "site-settings-update" => client.PutAsJsonAsync("/api/admin/site-settings", new
            {
                ownerName = "Auth Check Owner"
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, "Unsupported mutation.")
        };
    }

    private async Task<Blog> SeedBlogAsync(string title, bool published)
    {
        var now = DateTimeOffset.UtcNow;
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = $"{title.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
            Excerpt = $"{title} excerpt",
            Tags = ["seed"],
            Published = published,
            PublishedAt = published ? now : null,
            ContentJson = JsonSerializer.Serialize(new { html = $"<p>{title} content</p>" }),
            SearchTitle = title.ToLowerInvariant(),
            SearchText = $"{title} content",
            CreatedAt = now,
            UpdatedAt = now
        };

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        return blog;
    }

    private async Task<Work> SeedWorkAsync(string title, bool published)
    {
        var now = DateTimeOffset.UtcNow;
        var work = new Work
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = $"{title.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
            Excerpt = $"{title} excerpt",
            Category = "platform",
            Period = "2026.04",
            Tags = ["seed"],
            Published = published,
            PublishedAt = published ? now : null,
            ContentJson = JsonSerializer.Serialize(new { html = $"<p>{title} content</p>" }),
            AllPropertiesJson = "{}",
            SearchTitle = title.ToLowerInvariant(),
            SearchText = $"{title} content",
            CreatedAt = now,
            UpdatedAt = now
        };

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        dbContext.Works.Add(work);
        await dbContext.SaveChangesAsync();
        return work;
    }

    private async Task<SiteSetting> SetSiteSettingsAsync(
        string ownerName,
        string tagline,
        string facebookUrl,
        string instagramUrl,
        string githubUrl)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var settings = await dbContext.SiteSettings.SingleAsync(x => x.Singleton);
        settings.OwnerName = ownerName;
        settings.Tagline = tagline;
        settings.FacebookUrl = facebookUrl;
        settings.InstagramUrl = instagramUrl;
        settings.GitHubUrl = githubUrl;
        await dbContext.SaveChangesAsync();

        return new SiteSetting
        {
            Singleton = settings.Singleton,
            OwnerName = settings.OwnerName,
            Tagline = settings.Tagline,
            FacebookUrl = settings.FacebookUrl,
            InstagramUrl = settings.InstagramUrl,
            TwitterUrl = settings.TwitterUrl,
            LinkedInUrl = settings.LinkedInUrl,
            GitHubUrl = settings.GitHubUrl,
            ResumeAssetId = settings.ResumeAssetId,
            UpdatedAt = settings.UpdatedAt
        };
    }

    private async Task<int> CountBlogsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        return await dbContext.Blogs.CountAsync();
    }

    private async Task<int> CountWorksAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        return await dbContext.Works.CountAsync();
    }

    private sealed record CreatedPayload(Guid Id, string Slug);

    private sealed record SuccessPayload(bool Success);

    private sealed record CsrfPayload(string RequestToken, string HeaderName);
}
