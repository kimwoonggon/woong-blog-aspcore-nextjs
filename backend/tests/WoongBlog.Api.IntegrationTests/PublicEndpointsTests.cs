using System.Net;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Application.Modules.Composition.GetHome;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogs;
using WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;
using WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;
using WoongBlog.Application.Modules.Content.Works.GetWorks;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;
using WoongBlog.Application.Modules.Site.GetSiteSettings;
using WoongBlog.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Integration)]
public class PublicEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PublicEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetApiHealth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicSiteSettings_ReturnsSeededOwnerName()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/site-settings");

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Woonggon Kim", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicSiteSettings_ReturnsPublicDtoShape_ForAnonymousClient()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/site-settings");

        response.EnsureSuccessStatusCode();
        var (settings, root) = await ReadJsonWithRootAsync<SiteSettingsDto>(response);

        Assert.Equal("Woonggon Kim", settings.OwnerName);
        Assert.Equal("Creative Technologist", settings.Tagline);
        Assert.Equal("https://github.com/woong", settings.GitHubUrl);
        Assert.Equal("https://linkedin.com/in/woong", settings.LinkedInUrl);
        Assert.True(root.TryGetProperty("ownerName", out _));
        Assert.True(root.TryGetProperty("tagline", out _));
        Assert.True(root.TryGetProperty("gitHubUrl", out _));
        Assert.True(root.TryGetProperty("linkedInUrl", out _));
        Assert.False(root.TryGetProperty("resumeAssetId", out _));
    }

    [Fact]
    public async Task GetPublicHome_ReturnsFeaturedCollections()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/home");

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("homePage", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("featuredWorks", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("recentPosts", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicHome_ReturnsPublicDtoShape_ForAnonymousClient()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/home");

        response.EnsureSuccessStatusCode();
        var (home, root) = await ReadJsonWithRootAsync<HomeDto>(response);

        Assert.Equal("Home", home.HomePage.Title);
        Assert.Contains("Hi, I am Woonggon", home.HomePage.ContentJson, StringComparison.Ordinal);
        Assert.Equal("Woonggon Kim", home.SiteSettings.OwnerName);
        Assert.NotEmpty(home.FeaturedWorks);
        Assert.NotEmpty(home.RecentPosts);
        Assert.All(home.FeaturedWorks, work => Assert.NotNull(work.PublishedAt));
        Assert.All(home.RecentPosts, post => Assert.NotNull(post.PublishedAt));
        Assert.True(root.TryGetProperty("homePage", out _));
        Assert.True(root.TryGetProperty("siteSettings", out _));
        Assert.True(root.TryGetProperty("featuredWorks", out _));
        Assert.True(root.TryGetProperty("recentPosts", out _));
    }

    [Fact]
    public async Task GetPageBySlug_ReturnsSerializedPage_ForAnonymousClient()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/pages/introduction");

        response.EnsureSuccessStatusCode();
        var (page, root) = await ReadJsonWithRootAsync<PageDto>(response);

        Assert.Equal("introduction", page.Slug);
        Assert.Equal("Introduction", page.Title);
        Assert.Contains("product engineering", page.ContentJson, StringComparison.OrdinalIgnoreCase);
        Assert.True(root.TryGetProperty("slug", out _));
        Assert.True(root.TryGetProperty("title", out _));
        Assert.True(root.TryGetProperty("contentJson", out _));
    }

    [Fact]
    public async Task GetPageBySlug_ReturnsNotFound_WhenMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/pages/missing-page");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkBySlug_ReturnsSeededDetail()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/works/seeded-work");

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Portfolio Platform Rebuild", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("platform", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetWorkBySlug_ReturnsSerializedDetailWithMedia_ForAnonymousClient()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/works/seeded-work");

        response.EnsureSuccessStatusCode();
        var (work, root) = await ReadJsonWithRootAsync<WorkDetailDto>(response);

        Assert.Equal("seeded-work", work.Slug);
        Assert.Equal("Portfolio Platform Rebuild", work.Title);
        Assert.Equal("platform", work.Category);
        Assert.Contains("dotnet", work.Tags, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("/media/works/seeded-work-thumb.png", work.ThumbnailUrl);
        Assert.Equal("/media/works/seeded-work-icon.png", work.IconUrl);
        Assert.NotEmpty(work.Videos);
        Assert.Equal(new[] { "Seed Overview", "Seed Demo" }, work.Videos.Select(x => x.OriginalFileName).ToArray());
        Assert.False(root.TryGetProperty("contentJson", out _));
        Assert.True(root.TryGetProperty("content", out _));
        Assert.True(root.TryGetProperty("thumbnailUrl", out _));
        Assert.True(root.TryGetProperty("iconUrl", out _));
        Assert.True(root.TryGetProperty("videos", out _));
        Assert.True(root.TryGetProperty("videos_version", out _));
    }

    [Fact]
    public async Task GetWorkBySlug_ReturnsPublicContentBodyWithoutAdminContentJson()
    {
        var client = _factory.CreateClient();
        var slug = $"public-body-work-{Guid.NewGuid():N}";
        var publicToken = $"public-work-body-{Guid.NewGuid():N}";
        var adminOnlyToken = $"admin-only-work-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Works.Add(new Work
            {
                Id = Guid.NewGuid(),
                Title = "Public Body Work",
                Slug = slug,
                Excerpt = "public body",
                Category = "public",
                Tags = ["body"],
                ContentJson = JsonSerializer.Serialize(new
                {
                    html = $"<p>{publicToken}</p>",
                    editorState = adminOnlyToken
                }),
                AllPropertiesJson = "{}",
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/works/{slug}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.False(document.RootElement.TryGetProperty("contentJson", out _));
        Assert.True(document.RootElement.TryGetProperty("content", out var content));
        Assert.Equal($"<p>{publicToken}</p>", content.GetProperty("html").GetString());
        Assert.DoesNotContain(adminOnlyToken, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetWorkBySlug_CompressesPublicDetailJson_WhenClientAcceptsGzip()
    {
        var client = _factory.CreateClient();
        var slug = $"public-compressed-work-{Guid.NewGuid():N}";
        var publicToken = $"public-compressed-body-{Guid.NewGuid():N}";
        var repeatedBody = string.Concat(Enumerable.Repeat($"<p>{publicToken} compression-target body.</p>", 800));

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Works.Add(new Work
            {
                Id = Guid.NewGuid(),
                Title = "Public Compressed Work",
                Slug = slug,
                Excerpt = "public compressed body",
                Category = "public",
                Tags = ["compression"],
                ContentJson = JsonSerializer.Serialize(new { html = repeatedBody }),
                AllPropertiesJson = "{}",
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/public/works/{slug}");
        request.Headers.AcceptEncoding.ParseAdd("gzip");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Contains("gzip", response.Content.Headers.ContentEncoding);
        Assert.Contains("Accept-Encoding", response.Headers.Vary);

        var compressedBody = await response.Content.ReadAsByteArrayAsync();
        await using var compressedStream = new MemoryStream(compressedBody);
        await using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Contains(publicToken, body, StringComparison.Ordinal);
        Assert.True(compressedBody.Length < Encoding.UTF8.GetByteCount(body));
    }

    [Fact]
    public async Task GetWorkBySlug_OmitsNullOptionalVideoFields()
    {
        var client = _factory.CreateClient();
        var slug = $"public-video-null-payload-{Guid.NewGuid():N}";
        var workId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            var storedVideo = new WorkVideo
            {
                WorkId = workId,
                SourceType = WorkVideoSourceTypes.YouTube,
                SourceKey = "dQw4w9WgXcQ",
                SortOrder = 0,
                CreatedAt = now
            };
            dbContext.Works.Add(new Work
            {
                Id = workId,
                Title = "Public Video Null Payload Work",
                Slug = slug,
                Excerpt = "public video",
                Category = "public",
                Tags = ["video"],
                ContentJson = JsonSerializer.Serialize(new { html = "<p>Video body</p>" }),
                PublicContentHtml = "<p>Video body</p>",
                AllPropertiesJson = "{}",
                VideosVersion = 1,
                PublicVideosJson = WorkPublicVideosReadModel.Serialize([storedVideo]),
                Published = true,
                PublishedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
            dbContext.WorkVideos.Add(storedVideo);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/works/{slug}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var video = document.RootElement.GetProperty("videos")[0];
        Assert.Equal(WorkVideoSourceTypes.YouTube, video.GetProperty("sourceType").GetString());
        Assert.Equal("dQw4w9WgXcQ", video.GetProperty("sourceKey").GetString());
        Assert.True(video.TryGetProperty("sortOrder", out _));
        Assert.False(video.TryGetProperty("playbackUrl", out _));
        Assert.False(video.TryGetProperty("originalFileName", out _));
        Assert.False(video.TryGetProperty("mimeType", out _));
        Assert.False(video.TryGetProperty("fileSize", out _));
        Assert.False(video.TryGetProperty("width", out _));
        Assert.False(video.TryGetProperty("height", out _));
        Assert.False(video.TryGetProperty("duration_seconds", out _));
        Assert.False(video.TryGetProperty("timeline_preview_vtt_url", out _));
        Assert.False(video.TryGetProperty("timeline_preview_sprite_url", out _));
    }

    [Fact]
    public async Task GetWorkBySlug_ReturnsNotFound_WhenMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/works/missing-work");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkBySlug_ReturnsNotFound_WhenWorkIsDraft()
    {
        var client = _factory.CreateClient();
        var slug = $"draft-work-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Works.Add(new Work
            {
                Id = Guid.NewGuid(),
                Title = "Draft Work Detail",
                Slug = slug,
                Excerpt = "draft",
                Category = "draft",
                ContentJson = "{}",
                AllPropertiesJson = "{}",
                Published = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/works/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicWorks_FiltersDraftsOrdersByPublishedDateAndMapsAssets()
    {
        var client = _factory.CreateClient();
        var token = $"public-works-{Guid.NewGuid():N}";
        var newerTitle = $"{token} newer";
        var olderTitle = $"{token} older";
        var draftTitle = $"{token} draft";
        var thumbnailAssetId = Guid.NewGuid();
        var iconAssetId = Guid.NewGuid();
        var draftThumbnailAssetId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Assets.AddRange(
                new Asset { Id = thumbnailAssetId, Bucket = "media", Path = $"{token}-thumb.png", PublicUrl = $"/media/{token}-thumb.png" },
                new Asset { Id = iconAssetId, Bucket = "media", Path = $"{token}-icon.png", PublicUrl = $"/media/{token}-icon.png" },
                new Asset { Id = draftThumbnailAssetId, Bucket = "media", Path = $"{token}-draft.png", PublicUrl = $"/media/{token}-draft.png" });
            dbContext.Works.AddRange(
                new Work
                {
                    Id = Guid.NewGuid(),
                    Title = olderTitle,
                    Slug = $"{token}-older",
                    Excerpt = "older",
                    Category = "public",
                    Tags = ["public"],
                    ThumbnailAssetId = thumbnailAssetId,
                    IconAssetId = iconAssetId,
                    PublicThumbnailUrl = $"/media/{token}-thumb.png",
                    PublicIconUrl = $"/media/{token}-icon.png",
                    Published = true,
                    PublishedAt = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    ContentJson = "{}",
                    AllPropertiesJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Work
                {
                    Id = Guid.NewGuid(),
                    Title = newerTitle,
                    Slug = $"{token}-newer",
                    Excerpt = "newer",
                    Category = "public",
                    Tags = ["public"],
                    Published = true,
                    PublishedAt = new DateTimeOffset(2030, 2, 1, 0, 0, 0, TimeSpan.Zero),
                    ContentJson = "{}",
                    AllPropertiesJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Work
                {
                    Id = Guid.NewGuid(),
                    Title = draftTitle,
                    Slug = $"{token}-draft",
                    Excerpt = "draft",
                    Category = "public",
                    Tags = ["public"],
                    ThumbnailAssetId = draftThumbnailAssetId,
                    PublicThumbnailUrl = $"/media/{token}-draft.png",
                    Published = false,
                    PublishedAt = new DateTimeOffset(2031, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    ContentJson = "{}",
                    AllPropertiesJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/works?page=1&pageSize=10&query={Uri.EscapeDataString(token)}&searchMode=title");

        response.EnsureSuccessStatusCode();
        var page = await ReadJsonAsync<PagedWorksDto>(response);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(new[] { newerTitle, olderTitle }, page.Items.Select(x => x.Title).ToArray());
        Assert.DoesNotContain(page.Items, x => x.Title == draftTitle);
        Assert.Equal($"/media/{token}-thumb.png", page.Items[1].ThumbnailUrl);
        Assert.Equal($"/media/{token}-icon.png", page.Items[1].IconUrl);
    }

    [Fact]
    public async Task GetPublicWorks_ReturnsPagedPayloadShape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/works?page=1&pageSize=12");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"items\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"page\":1", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"pageSize\":12", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"contentJson\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicWorks_FiltersByTitleSearch()
    {
        var client = _factory.CreateClient();
        var uniqueTitle = $"Searchable Work Title {Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Works.Add(new Work
            {
                Id = Guid.NewGuid(),
                Title = uniqueTitle,
                Slug = $"search-work-title-{Guid.NewGuid():N}",
                Excerpt = "Title search target excerpt",
                Category = "search",
                Tags = ["search"],
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow.AddYears(-1),
                ContentJson = JsonSerializer.Serialize(new { html = "<p>Body without the title query.</p>" }),
                AllPropertiesJson = "{}",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/works?page=1&pageSize=12&query={Uri.EscapeDataString(uniqueTitle)}&searchMode=title");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(uniqueTitle, body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"totalItems\":1", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicWorks_FiltersByContentSearch()
    {
        var client = _factory.CreateClient();
        var uniqueBody = $"work-content-token-{Guid.NewGuid():N}";
        var title = $"Content Search Work {Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Works.Add(new Work
            {
                Id = Guid.NewGuid(),
                Title = title,
                Slug = $"search-work-content-{Guid.NewGuid():N}",
                Excerpt = $"Content search target excerpt {uniqueBody}",
                Category = "search",
                Tags = ["search"],
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow.AddYears(-1),
                ContentJson = JsonSerializer.Serialize(new { html = $"<p>{uniqueBody}</p>" }),
                AllPropertiesJson = "{}",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/works?page=1&pageSize=12&query={Uri.EscapeDataString(uniqueBody)}&searchMode=content");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(title, body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"totalItems\":1", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicWorks_QueryOnly_UsesUnifiedSearch()
    {
        var client = _factory.CreateClient();
        var bodyToken = $"works-unified-{Guid.NewGuid():N}";
        var title = $"Unified Works Title {Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Works.AddRange(
                new Work
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Slug = $"unified-works-title-{Guid.NewGuid():N}",
                    Excerpt = "title only",
                    Category = "search",
                    Tags = ["search"],
                    Published = true,
                    PublishedAt = DateTimeOffset.UtcNow.AddYears(-1),
                    ContentJson = JsonSerializer.Serialize(new { html = "<p>plain body</p>" }),
                    AllPropertiesJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Work
                {
                    Id = Guid.NewGuid(),
                    Title = $"Other Work {Guid.NewGuid():N}",
                    Slug = $"unified-works-body-{Guid.NewGuid():N}",
                    Excerpt = $"contains {bodyToken}",
                    Category = "search",
                    Tags = ["search"],
                    Published = true,
                    PublishedAt = DateTimeOffset.UtcNow.AddYears(-1),
                    ContentJson = JsonSerializer.Serialize(new { html = $"<p>{bodyToken}</p>" }),
                    AllPropertiesJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            await dbContext.SaveChangesAsync();
        }

        var titleResponse = await client.GetAsync($"/api/public/works?page=1&pageSize=12&query={Uri.EscapeDataString(title)}");
        titleResponse.EnsureSuccessStatusCode();
        var titleBody = await titleResponse.Content.ReadAsStringAsync();
        Assert.Contains(title, titleBody, StringComparison.OrdinalIgnoreCase);

        var contentResponse = await client.GetAsync($"/api/public/works?page=1&pageSize=12&query={Uri.EscapeDataString(bodyToken)}");
        contentResponse.EnsureSuccessStatusCode();
        var contentBody = await contentResponse.Content.ReadAsStringAsync();
        Assert.Contains(bodyToken, contentBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicBlogs_ReturnsPagedPayloadShape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/blogs?page=1&pageSize=12");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"items\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"page\":1", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"pageSize\":12", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"contentJson\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicBlogs_FiltersByTitleSearch()
    {
        var client = _factory.CreateClient();
        var uniqueTitle = $"Searchable Study Title {Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Blogs.Add(new Blog
            {
                Id = Guid.NewGuid(),
                Title = uniqueTitle,
                Slug = $"search-title-{Guid.NewGuid():N}",
                Excerpt = "Title search target excerpt",
                Tags = ["search"],
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow.AddYears(-1),
                ContentJson = JsonSerializer.Serialize(new { html = "<p>Body without the title query.</p>" }),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/blogs?page=1&pageSize=12&query={Uri.EscapeDataString(uniqueTitle)}&searchMode=title");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(uniqueTitle, body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"totalItems\":1", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicBlogs_FiltersByContentSearch()
    {
        var client = _factory.CreateClient();
        var uniqueBody = $"content-token-{Guid.NewGuid():N}";
        var title = $"Content Search Study {Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Blogs.Add(new Blog
            {
                Id = Guid.NewGuid(),
                Title = title,
                Slug = $"search-content-{Guid.NewGuid():N}",
                Excerpt = $"Content search target excerpt {uniqueBody}",
                Tags = ["search"],
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow.AddYears(-1),
                ContentJson = JsonSerializer.Serialize(new { html = $"<p>{uniqueBody}</p>" }),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/blogs?page=1&pageSize=12&query={Uri.EscapeDataString(uniqueBody)}&searchMode=content");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(title, body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"totalItems\":1", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicBlogs_QueryOnly_UsesUnifiedSearch()
    {
        var client = _factory.CreateClient();
        var bodyToken = $"blogs-unified-{Guid.NewGuid():N}";
        var title = $"Unified Blogs Title {Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Blogs.AddRange(
                new Blog
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Slug = $"unified-blogs-title-{Guid.NewGuid():N}",
                    Excerpt = "title only",
                    Tags = ["search"],
                    Published = true,
                    PublishedAt = DateTimeOffset.UtcNow.AddYears(-1),
                    ContentJson = JsonSerializer.Serialize(new { html = "<p>plain body</p>" }),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Blog
                {
                    Id = Guid.NewGuid(),
                    Title = $"Other Blog {Guid.NewGuid():N}",
                    Slug = $"unified-blogs-body-{Guid.NewGuid():N}",
                    Excerpt = $"contains {bodyToken}",
                    Tags = ["search"],
                    Published = true,
                    PublishedAt = DateTimeOffset.UtcNow.AddYears(-1),
                    ContentJson = JsonSerializer.Serialize(new { html = $"<p>{bodyToken}</p>" }),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            await dbContext.SaveChangesAsync();
        }

        var titleResponse = await client.GetAsync($"/api/public/blogs?page=1&pageSize=12&query={Uri.EscapeDataString(title)}");
        titleResponse.EnsureSuccessStatusCode();
        var titleBody = await titleResponse.Content.ReadAsStringAsync();
        Assert.Contains(title, titleBody, StringComparison.OrdinalIgnoreCase);

        var contentResponse = await client.GetAsync($"/api/public/blogs?page=1&pageSize=12&query={Uri.EscapeDataString(bodyToken)}");
        contentResponse.EnsureSuccessStatusCode();
        var contentBody = await contentResponse.Content.ReadAsStringAsync();
        Assert.Contains(bodyToken, contentBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsNotFound_WhenMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/blogs/missing-blog");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsSerializedDetailWithCover_ForAnonymousClient()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/blogs/seeded-blog");

        response.EnsureSuccessStatusCode();
        var (blog, root) = await ReadJsonWithRootAsync<BlogDetailDto>(response);

        Assert.Equal("seeded-blog", blog.Slug);
        Assert.Equal("Designing a Seed-First Migration Strategy", blog.Title);
        Assert.Contains("architecture", blog.Tags, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("/media/blogs/seeded-blog-cover.png", blog.CoverUrl);
        Assert.Contains("Seed data", blog.Content.Html, StringComparison.OrdinalIgnoreCase);
        Assert.False(root.TryGetProperty("contentJson", out _));
        Assert.True(root.TryGetProperty("content", out _));
        Assert.True(root.TryGetProperty("coverUrl", out _));
        Assert.True(root.TryGetProperty("publishedAt", out _));
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsPublicContentBodyWithoutAdminContentJson()
    {
        var client = _factory.CreateClient();
        var slug = $"public-body-blog-{Guid.NewGuid():N}";
        var publicToken = $"public-blog-body-{Guid.NewGuid():N}";
        var adminOnlyToken = $"admin-only-blog-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Blogs.Add(new Blog
            {
                Id = Guid.NewGuid(),
                Title = "Public Body Blog",
                Slug = slug,
                Excerpt = "public body",
                Tags = ["body"],
                ContentJson = JsonSerializer.Serialize(new
                {
                    html = $"<p>{publicToken}</p>",
                    editorState = adminOnlyToken
                }),
                Published = true,
                PublishedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/blogs/{slug}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.False(document.RootElement.TryGetProperty("contentJson", out _));
        Assert.True(document.RootElement.TryGetProperty("content", out var content));
        Assert.Equal($"<p>{publicToken}</p>", content.GetProperty("html").GetString());
        Assert.DoesNotContain(adminOnlyToken, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsNotFound_WhenBlogIsDraft()
    {
        var client = _factory.CreateClient();
        var slug = $"draft-blog-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Blogs.Add(new Blog
            {
                Id = Guid.NewGuid(),
                Title = "Draft Blog Detail",
                Slug = slug,
                Excerpt = "draft",
                ContentJson = "{}",
                Published = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/blogs/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicBlogs_FiltersDraftsOrdersByPublishedDateAndMapsAssets()
    {
        var client = _factory.CreateClient();
        var token = $"public-blogs-{Guid.NewGuid():N}";
        var newerTitle = $"{token} newer";
        var olderTitle = $"{token} older";
        var draftTitle = $"{token} draft";
        var coverAssetId = Guid.NewGuid();
        var draftCoverAssetId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.Assets.AddRange(
                new Asset { Id = coverAssetId, Bucket = "media", Path = $"{token}-cover.png", PublicUrl = $"/media/{token}-cover.png" },
                new Asset { Id = draftCoverAssetId, Bucket = "media", Path = $"{token}-draft-cover.png", PublicUrl = $"/media/{token}-draft-cover.png" });
            dbContext.Blogs.AddRange(
                new Blog
                {
                    Id = Guid.NewGuid(),
                    Title = olderTitle,
                    Slug = $"{token}-older",
                    Excerpt = "older",
                    Tags = ["public"],
                    CoverAssetId = coverAssetId,
                    PublicCoverUrl = $"/media/{token}-cover.png",
                    Published = true,
                    PublishedAt = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    ContentJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Blog
                {
                    Id = Guid.NewGuid(),
                    Title = newerTitle,
                    Slug = $"{token}-newer",
                    Excerpt = "newer",
                    Tags = ["public"],
                    Published = true,
                    PublishedAt = new DateTimeOffset(2030, 2, 1, 0, 0, 0, TimeSpan.Zero),
                    ContentJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Blog
                {
                    Id = Guid.NewGuid(),
                    Title = draftTitle,
                    Slug = $"{token}-draft",
                    Excerpt = "draft",
                    Tags = ["public"],
                    CoverAssetId = draftCoverAssetId,
                    PublicCoverUrl = $"/media/{token}-draft-cover.png",
                    Published = false,
                    PublishedAt = new DateTimeOffset(2031, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    ContentJson = "{}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/public/blogs?page=1&pageSize=10&query={Uri.EscapeDataString(token)}&searchMode=title");

        response.EnsureSuccessStatusCode();
        var page = await ReadJsonAsync<PagedBlogsDto>(response);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(new[] { newerTitle, olderTitle }, page.Items.Select(x => x.Title).ToArray());
        Assert.DoesNotContain(page.Items, x => x.Title == draftTitle);
        Assert.Equal($"/media/{token}-cover.png", page.Items[1].CoverUrl);
    }

    [Fact]
    public async Task GetPublicListEndpoints_ReturnStableEmptyResponses_WhenContentTablesAreEmpty()
    {
        using var emptyFactory = new CustomWebApplicationFactory();
        var client = emptyFactory.CreateClient();

        using (var scope = emptyFactory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            dbContext.WorkVideos.RemoveRange(dbContext.WorkVideos);
            dbContext.Works.RemoveRange(dbContext.Works);
            dbContext.Blogs.RemoveRange(dbContext.Blogs);
            await dbContext.SaveChangesAsync();
        }

        var worksResponse = await client.GetAsync("/api/public/works?page=99&pageSize=2");
        var blogsResponse = await client.GetAsync("/api/public/blogs?page=99&pageSize=2");

        worksResponse.EnsureSuccessStatusCode();
        blogsResponse.EnsureSuccessStatusCode();
        var works = await ReadJsonAsync<PagedWorksDto>(worksResponse);
        var blogs = await ReadJsonAsync<PagedBlogsDto>(blogsResponse);
        Assert.Empty(works.Items);
        Assert.Equal(1, works.Page);
        Assert.Equal(2, works.PageSize);
        Assert.Equal(0, works.TotalItems);
        Assert.Equal(1, works.TotalPages);
        Assert.Empty(blogs.Items);
        Assert.Equal(1, blogs.Page);
        Assert.Equal(2, blogs.PageSize);
        Assert.Equal(0, blogs.TotalItems);
        Assert.Equal(1, blogs.TotalPages);
    }

    [Fact]
    public async Task GetPublicResume_ReturnsSeededResumeUrl()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/resume");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("/media/resume/woonggon-kim-resume.pdf", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SeededAdminProfile_Exists()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();

        var adminProfile = await dbContext.Profiles.FindAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.NotNull(adminProfile);
        Assert.Equal("admin", adminProfile!.Role);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
        where T : class
    {
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(body, JsonOptions);
        Assert.NotNull(result);
        return result!;
    }

    private static async Task<(T Payload, JsonElement Root)> ReadJsonWithRootAsync<T>(HttpResponseMessage response)
        where T : class
    {
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(body, JsonOptions);
        Assert.NotNull(result);
        using var document = JsonDocument.Parse(body);
        return (result!, document.RootElement.Clone());
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
