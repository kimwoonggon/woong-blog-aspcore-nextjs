using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Tests;

public class WorkVideoEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkVideoEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddYouTubeVideo_PersistsAndProjectsToAdminAndPublic()
    {
        var client = _factory.CreateAuthenticatedClient();
        var created = await CreateWorkAsync(client, $"YouTube Work {Guid.NewGuid():N}");

        var addResponse = await client.PostAsJsonAsync($"/api/admin/works/{created.Id}/videos/youtube", new
        {
            youtubeUrlOrId = "https://youtu.be/dQw4w9WgXcQ",
            expectedVideosVersion = 0
        });

        addResponse.EnsureSuccessStatusCode();
        var addBody = await addResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"videos_version\":1", addBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dQw4w9WgXcQ", addBody, StringComparison.OrdinalIgnoreCase);

        var adminResponse = await client.GetAsync($"/api/admin/works/{created.Id}");
        adminResponse.EnsureSuccessStatusCode();
        var adminBody = await adminResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"sourceType\":\"youtube\"", adminBody, StringComparison.OrdinalIgnoreCase);

        var publicResponse = await client.GetAsync($"/api/public/works/{created.Slug}");
        publicResponse.EnsureSuccessStatusCode();
        var publicBody = await publicResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"videos\":[", publicBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dQw4w9WgXcQ", publicBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LocalUploadConfirmAndDeleteWork_EnqueuesCleanupAndRemovesVideoRows()
    {
        var client = _factory.CreateAuthenticatedClient();
        var created = await CreateWorkAsync(client, $"Upload Work {Guid.NewGuid():N}");

        var uploadUrlResponse = await client.PostAsJsonAsync($"/api/admin/works/{created.Id}/videos/upload-url", new
        {
            fileName = "demo.mp4",
            contentType = "video/mp4",
            size = SampleMp4Bytes.Length,
            expectedVideosVersion = 0
        });
        uploadUrlResponse.EnsureSuccessStatusCode();
        var uploadTarget = await uploadUrlResponse.Content.ReadFromJsonAsync<UploadTargetPayload>();
        Assert.NotNull(uploadTarget);

        using (var form = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(SampleMp4Bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            form.Add(fileContent, "file", "demo.mp4");

            var uploadResponse = await client.PostAsync($"/api/admin/works/{created.Id}/videos/upload?uploadSessionId={uploadTarget!.UploadSessionId}", form);
            uploadResponse.EnsureSuccessStatusCode();
        }

        var confirmResponse = await client.PostAsJsonAsync($"/api/admin/works/{created.Id}/videos/confirm", new
        {
            uploadSessionId = uploadTarget!.UploadSessionId,
            expectedVideosVersion = 0
        });
        confirmResponse.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            Assert.Single(dbContext.WorkVideos.Where(x => x.WorkId == created.Id));
        }

        var deleteResponse = await client.DeleteAsync($"/api/admin/works/{created.Id}");
        deleteResponse.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            Assert.Null(dbContext.Works.SingleOrDefault(x => x.Id == created.Id));
            Assert.Empty(dbContext.WorkVideos.Where(x => x.WorkId == created.Id));
            Assert.Empty(dbContext.WorkVideoUploadSessions.Where(x => x.WorkId == created.Id));
            Assert.NotEmpty(dbContext.VideoStorageCleanupJobs.Where(x => x.WorkId == created.Id));
        }
    }

    [Fact]
    public async Task ReorderWorkVideos_ReturnsConflictWhenVideosVersionIsStale()
    {
        var client = _factory.CreateAuthenticatedClient();
        var created = await CreateWorkAsync(client, $"Reorder Work {Guid.NewGuid():N}");

        var addResponse = await client.PostAsJsonAsync($"/api/admin/works/{created.Id}/videos/youtube", new
        {
            youtubeUrlOrId = "dQw4w9WgXcQ",
            expectedVideosVersion = 0
        });
        addResponse.EnsureSuccessStatusCode();
        var payload = await addResponse.Content.ReadFromJsonAsync<MutationPayload>();
        Assert.NotNull(payload);

        var reorderResponse = await client.PutAsJsonAsync($"/api/admin/works/{created.Id}/videos/order", new
        {
            orderedVideoIds = payload!.Videos.Select(video => video.Id).ToArray(),
            expectedVideosVersion = 0
        });

        Assert.Equal(HttpStatusCode.Conflict, reorderResponse.StatusCode);
    }

    [Fact]
    public async Task ReorderWorkVideos_PersistsUpdatedPublicAndAdminOrder()
    {
        var client = _factory.CreateAuthenticatedClient();
        var created = await CreateWorkAsync(client, $"Reorder Persisted Work {Guid.NewGuid():N}");

        var firstAdd = await client.PostAsJsonAsync($"/api/admin/works/{created.Id}/videos/youtube", new
        {
            youtubeUrlOrId = "dQw4w9WgXcQ",
            expectedVideosVersion = 0
        });
        firstAdd.EnsureSuccessStatusCode();
        var firstPayload = await firstAdd.Content.ReadFromJsonAsync<MutationPayload>();
        Assert.NotNull(firstPayload);

        var secondAdd = await client.PostAsJsonAsync($"/api/admin/works/{created.Id}/videos/youtube", new
        {
            youtubeUrlOrId = "9bZkp7q19f0",
            expectedVideosVersion = firstPayload!.VideosVersion
        });
        secondAdd.EnsureSuccessStatusCode();
        var secondPayload = await secondAdd.Content.ReadFromJsonAsync<MutationPayload>();
        Assert.NotNull(secondPayload);
        Assert.Equal(2, secondPayload!.Videos.Length);

        var reorderedIds = secondPayload.Videos.Select(video => video.Id).Reverse().ToArray();
        var reorderResponse = await client.PutAsJsonAsync($"/api/admin/works/{created.Id}/videos/order", new
        {
            orderedVideoIds = reorderedIds,
            expectedVideosVersion = secondPayload.VideosVersion
        });

        reorderResponse.EnsureSuccessStatusCode();
        var reorderPayload = await reorderResponse.Content.ReadFromJsonAsync<MutationPayload>();
        Assert.NotNull(reorderPayload);
        Assert.Equal(reorderedIds[0], reorderPayload!.Videos[0].Id);
        Assert.Equal(reorderedIds[1], reorderPayload.Videos[1].Id);

        var adminResponse = await client.GetAsync($"/api/admin/works/{created.Id}");
        adminResponse.EnsureSuccessStatusCode();
        var adminBody = await adminResponse.Content.ReadAsStringAsync();
        var firstIndex = adminBody.IndexOf("9bZkp7q19f0", StringComparison.Ordinal);
        var secondIndex = adminBody.IndexOf("dQw4w9WgXcQ", StringComparison.Ordinal);
        Assert.True(firstIndex >= 0 && secondIndex >= 0 && firstIndex < secondIndex);

        var publicResponse = await client.GetAsync($"/api/public/works/{created.Slug}");
        publicResponse.EnsureSuccessStatusCode();
        var publicBody = await publicResponse.Content.ReadAsStringAsync();
        var publicFirstIndex = publicBody.IndexOf("9bZkp7q19f0", StringComparison.Ordinal);
        var publicSecondIndex = publicBody.IndexOf("dQw4w9WgXcQ", StringComparison.Ordinal);
        Assert.True(publicFirstIndex >= 0 && publicSecondIndex >= 0 && publicFirstIndex < publicSecondIndex);
    }

    [Fact]
    public async Task ExpireUploadSessions_MarksSessionAndEnqueuesCleanup()
    {
        var client = _factory.CreateAuthenticatedClient();
        var created = await CreateWorkAsync(client, $"Expiry Work {Guid.NewGuid():N}");

        var uploadUrlResponse = await client.PostAsJsonAsync($"/api/admin/works/{created.Id}/videos/upload-url", new
        {
            fileName = "demo.mp4",
            contentType = "video/mp4",
            size = SampleMp4Bytes.Length,
            expectedVideosVersion = 0
        });
        uploadUrlResponse.EnsureSuccessStatusCode();
        var uploadTarget = await uploadUrlResponse.Content.ReadFromJsonAsync<UploadTargetPayload>();
        Assert.NotNull(uploadTarget);
        var confirmedTarget = uploadTarget!;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IWorkVideoService>();
        var session = dbContext.WorkVideoUploadSessions.Single(x => x.Id == confirmedTarget.UploadSessionId);
        session.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        await service.ExpireUploadSessionsAsync(CancellationToken.None);

        Assert.Equal(WorkVideoUploadSessionStatuses.Expired, dbContext.WorkVideoUploadSessions.Single(x => x.Id == confirmedTarget.UploadSessionId).Status);
        Assert.NotEmpty(dbContext.VideoStorageCleanupJobs.Where(x => x.StorageKey == confirmedTarget.StorageKey));
    }

    private static async Task<CreatedWorkPayload> CreateWorkAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/admin/works", new
        {
            title,
            category = "video",
            period = "2026.04",
            tags = new[] { "video" },
            published = true,
            contentJson = "{\"html\":\"<p>Body</p>\"}",
            allPropertiesJson = "{}"
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreatedWorkPayload>();
        return payload!;
    }

    private sealed class CreatedWorkPayload
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
    }

    private sealed class UploadTargetPayload
    {
        public Guid UploadSessionId { get; set; }
        public string UploadMethod { get; set; } = string.Empty;
        public string UploadUrl { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
    }

    private sealed class MutationPayload
    {
        [JsonPropertyName("videos_version")]
        public int VideosVersion { get; set; }
        public VideoPayload[] Videos { get; set; } = [];
    }

    private sealed class VideoPayload
    {
        public Guid Id { get; set; }
    }

    private static readonly byte[] SampleMp4Bytes =
    [
        0x00, 0x00, 0x00, 0x18,
        0x66, 0x74, 0x79, 0x70,
        0x6D, 0x70, 0x34, 0x32,
        0x00, 0x00, 0x00, 0x00,
        0x6D, 0x70, 0x34, 0x32,
        0x69, 0x73, 0x6F, 0x6D
    ];
}
