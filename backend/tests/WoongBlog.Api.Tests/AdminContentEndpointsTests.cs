using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WoongBlog.Api.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

public class AdminContentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminContentEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAdminPages_WithSlugFilter_ReturnsRequestedPageOnly()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/admin/pages?slugs=introduction");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("introduction", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contact", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAdminPages_Home_ReturnsStructuredHomeContent()
    {
        var client = _factory.CreateAuthenticatedClient();
        var headline = $"Headline {Guid.NewGuid():N}";
        var introText = $"Intro {Guid.NewGuid():N}";
        var profileImageUrl = $"/media/public-assets/{Guid.NewGuid():N}.png";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            var home = dbContext.Pages.Single(x => x.Slug == "home");
            home.ContentJson = JsonSerializer.Serialize(new
            {
                headline,
                introText,
                profileImageUrl
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/admin/pages?slugs=home");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var homePage = document.RootElement.EnumerateArray().Single();
        var content = homePage.GetProperty("content");

        Assert.Equal(headline, content.GetProperty("headline").GetString());
        Assert.Equal(introText, content.GetProperty("introText").GetString());
        Assert.Equal(profileImageUrl, content.GetProperty("profileImageUrl").GetString());
    }

    [Fact]
    public async Task UpdateAdminPage_ReturnsBadRequest_WhenIdMissing()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync("/api/admin/pages", new
        {
            id = Guid.Empty,
            title = "Broken"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAdminPage_ReturnsNotFound_WhenPageMissing()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync("/api/admin/pages", new
        {
            id = Guid.NewGuid(),
            title = "Missing",
            contentJson = "{\"html\":\"<p>Missing</p>\"}"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSiteSettings_PersistsOwnerName()
    {
        var client = _factory.CreateAuthenticatedClient();
        var ownerName = $"Owner {Guid.NewGuid():N}";

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName,
            tagline = "Updated tagline"
        });

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var settings = dbContext.SiteSettings.Single(x => x.Singleton);
        Assert.Equal(ownerName, settings.OwnerName);
    }

    [Fact]
    public async Task UpdateSiteSettings_ClearsResumeAsset_WhenExplicitlyNull()
    {
        var client = _factory.CreateAuthenticatedClient();

        using (var initialScope = _factory.Services.CreateScope())
        {
            var initialDb = initialScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            var initialSettings = initialDb.SiteSettings.Single(x => x.Singleton);
            initialSettings.ResumeAssetId = Guid.NewGuid();
            await initialDb.SaveChangesAsync();
            Assert.NotNull(initialSettings.ResumeAssetId);
        }

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            resumeAssetId = (Guid?)null
        });

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var settings = dbContext.SiteSettings.Single(x => x.Singleton);

        Assert.Null(settings.ResumeAssetId);
    }

    [Fact]
    public async Task UpdateSiteSettings_PreservesResumeAsset_WhenResumeAssetIdIsOmitted()
    {
        var client = _factory.CreateAuthenticatedClient();
        var resumeAssetId = Guid.NewGuid();

        using (var initialScope = _factory.Services.CreateScope())
        {
            var initialDb = initialScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            var initialSettings = initialDb.SiteSettings.Single(x => x.Singleton);
            initialSettings.ResumeAssetId = resumeAssetId;
            await initialDb.SaveChangesAsync();
        }

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName = "Owner without touching resume"
        });

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var settings = dbContext.SiteSettings.Single(x => x.Singleton);

        Assert.Equal(resumeAssetId, settings.ResumeAssetId);
    }

    [Fact]
    public async Task UpdateSiteSettings_ReturnsBadRequest_WhenResumeAssetIdIsEmptyGuid()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            resumeAssetId = Guid.Empty
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBlog_ReturnsBadRequest_WhenTitleMissing()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "",
            tags = Array.Empty<string>(),
            published = true,
            contentJson = "{\"html\":\"<p>Body</p>\"}"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBlog_CreatesSlugAndExcerpt()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "Hello Integration Blog!!!",
            tags = new[] { "qa", "backend" },
            published = true,
            contentJson = "{\"html\":\"<p>This is a long enough body to become the excerpt.</p>\"}"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("hello-integration-blog", body, StringComparison.OrdinalIgnoreCase);

        var publicResponse = await client.GetAsync("/api/public/blogs/hello-integration-blog");
        publicResponse.EnsureSuccessStatusCode();
        var publicBody = await publicResponse.Content.ReadAsStringAsync();
        Assert.Contains("This is a long enough body", publicBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBlog_WithMarkdownOnlyContent_GeneratesExcerpt()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "Markdown Integration Blog",
            tags = new[] { "qa", "markdown" },
            published = true,
            contentJson = "{\"markdown\":\"## Markdown Heading\\n\\nBody copied from markdown with ![hero](/media/hero.png)\"}"
        });

        response.EnsureSuccessStatusCode();

        var publicResponse = await client.GetAsync("/api/public/blogs/markdown-integration-blog");
        publicResponse.EnsureSuccessStatusCode();
        var publicBody = await publicResponse.Content.ReadAsStringAsync();
        Assert.Contains("Markdown Heading Body copied from markdown with hero", publicBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBlog_WithWrappedMarkdownHtml_GeneratesExcerpt()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "Wrapped Markdown Blog",
            tags = new[] { "qa", "wrapped" },
            published = true,
            contentJson = "{\"html\":\"<p>## Wrapped Heading</p><p>Body copied from wrapped markdown</p>\"}"
        });

        response.EnsureSuccessStatusCode();

        var publicResponse = await client.GetAsync("/api/public/blogs/wrapped-markdown-blog");
        publicResponse.EnsureSuccessStatusCode();
        var publicBody = await publicResponse.Content.ReadAsStringAsync();
        Assert.Contains("Wrapped Heading Body copied from wrapped markdown", publicBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBlog_WithDuplicateTitle_GeneratesUniqueSlug()
    {
        var client = _factory.CreateAuthenticatedClient();

        var firstResponse = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "Duplicate Blog Title",
            tags = new[] { "qa" },
            published = true,
            contentJson = "{\"html\":\"<p>First body</p>\"}"
        });
        firstResponse.EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync("/api/admin/blogs", new
        {
            title = "Duplicate Blog Title",
            tags = new[] { "qa" },
            published = true,
            contentJson = "{\"html\":\"<p>Second body</p>\"}"
        });
        secondResponse.EnsureSuccessStatusCode();

        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("duplicate-blog-title-2", secondBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBlog_ReturnsNotFound_WhenBlogMissing()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync($"/api/admin/blogs/{Guid.NewGuid()}", new
        {
            title = "Missing",
            tags = Array.Empty<string>(),
            published = false,
            contentJson = "{\"html\":\"<p>Body</p>\"}"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateWork_ReturnsBadRequest_WhenCategoryMissing()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title = "Integration Work",
            category = "",
            period = "2026.03",
            tags = Array.Empty<string>(),
            published = true,
            contentJson = "{\"html\":\"<p>Body</p>\"}",
            allPropertiesJson = "{}"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndReadWork_FallsBackToEmptyObject_ForMalformedMetadata()
    {
        var client = _factory.CreateAuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title = "Weird JSON Work",
            category = "platform",
            period = "2026.03",
            tags = new[] { "qa" },
            published = true,
            contentJson = "{\"html\":\"<p>Visible body</p>\"}",
            allPropertiesJson = "{not-json}"
        });

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResource>();
        Assert.NotNull(created);

        var response = await client.GetAsync($"/api/admin/works/{created!.Id}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"all_properties\":{}", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateWork_WithDuplicateTitle_GeneratesUniqueSlug()
    {
        var client = _factory.CreateAuthenticatedClient();

        var firstResponse = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title = "Duplicate Work Title",
            category = "platform",
            period = "2026.03",
            tags = new[] { "qa" },
            published = true,
            contentJson = "{\"html\":\"<p>First body</p>\"}",
            allPropertiesJson = "{}"
        });
        firstResponse.EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title = "Duplicate Work Title",
            category = "platform",
            period = "2026.03",
            tags = new[] { "qa" },
            published = true,
            contentJson = "{\"html\":\"<p>Second body</p>\"}",
            allPropertiesJson = "{}"
        });
        secondResponse.EnsureSuccessStatusCode();

        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("duplicate-work-title-2", secondBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateWork_ReturnsNotFound_WhenWorkMissing()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync($"/api/admin/works/{Guid.NewGuid()}", new
        {
            title = "Missing",
            category = "platform",
            period = "2026.03",
            tags = Array.Empty<string>(),
            published = false,
            contentJson = "{\"html\":\"<p>Body</p>\"}",
            allPropertiesJson = "{}"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndReadWork_PersistsUploadedThumbnailAndIcon()
    {
        var client = _factory.CreateAuthenticatedClient();

        static MultipartFormDataContent CreateImageUploadForm(string bucket, string fileName)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(new MemoryStream(new byte[] { 137, 80, 78, 71 }));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(bucket), "bucket");
            return form;
        }

        using var thumbnailForm = CreateImageUploadForm("work-thumbnails", "thumb.png");
        using var iconForm = CreateImageUploadForm("work-icons", "icon.png");

        var thumbnailUpload = await client.PostAsync("/api/uploads", thumbnailForm);
        thumbnailUpload.EnsureSuccessStatusCode();
        var thumbnailPayload = await thumbnailUpload.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        var iconUpload = await client.PostAsync("/api/uploads", iconForm);
        iconUpload.EnsureSuccessStatusCode();
        var iconPayload = await iconUpload.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        var createResponse = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title = "Media-backed Work",
            category = "platform",
            period = "2026.03",
            tags = new[] { "qa", "media" },
            published = true,
            contentJson = "{\"html\":\"<p>Body with media</p>\"}",
            allPropertiesJson = "{}",
            thumbnailAssetId = thumbnailPayload!["id"],
            iconAssetId = iconPayload!["id"]
        });

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResource>();
        Assert.NotNull(created);

        var adminResponse = await client.GetAsync($"/api/admin/works/{created!.Id}");
        adminResponse.EnsureSuccessStatusCode();
        var adminBody = await adminResponse.Content.ReadAsStringAsync();
        Assert.Contains("/media/work-thumbnails/", adminBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/media/work-icons/", adminBody, StringComparison.OrdinalIgnoreCase);

        var publicResponse = await client.GetAsync($"/api/public/works/{created.Slug}");
        publicResponse.EnsureSuccessStatusCode();
        var publicBody = await publicResponse.Content.ReadAsStringAsync();
        Assert.Contains("/media/work-thumbnails/", publicBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/media/work-icons/", publicBody, StringComparison.OrdinalIgnoreCase);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var work = dbContext.Works.Single(x => x.Id == created.Id);
        Assert.Equal(Guid.Parse(thumbnailPayload["id"]), work.ThumbnailAssetId);
        Assert.Equal(Guid.Parse(iconPayload["id"]), work.IconAssetId);
    }

    private sealed class CreatedResource
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
    }
}
