using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

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
    public async Task FfmpegVideoTranscoder_SegmentsHlsAndProducesTheManifest()
    {
        var fakeFfTools = CreateFakeFfTools();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"portfolio-tests-hls-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDirectory);
            var inputPath = Path.Combine(tempDirectory, "source.mp4");
            var hlsDirectory = Path.Combine(tempDirectory, "hls");

            await File.WriteAllBytesAsync(inputPath, SampleMp4Bytes);
            Directory.CreateDirectory(hlsDirectory);

            var transcoder = new FfmpegVideoTranscoder(Options.Create(new WorkVideoHlsOptions
            {
                FfmpegPath = fakeFfTools.FfmpegPath,
                FfprobePath = fakeFfTools.FfprobePath,
                SegmentDurationSeconds = 4,
                TimelinePreviewIntervalSeconds = 5,
                TimelinePreviewTileColumns = 4,
                TimeoutSeconds = 30
            }));

            var error = await transcoder.SegmentHlsAsync(inputPath, hlsDirectory, "master.m3u8", CancellationToken.None);

            Assert.Null(error);
            Assert.True(File.Exists(Path.Combine(hlsDirectory, "master.m3u8")));
            Assert.True(File.Exists(Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewSpriteFileName)));
            var vttPath = Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewVttFileName);
            Assert.True(File.Exists(vttPath));
            var vtt = await File.ReadAllTextAsync(vttPath);
            Assert.Contains("00:00:00.000 --> 00:00:05.000", vtt, StringComparison.Ordinal);
            Assert.Contains("00:00:15.000 --> 00:00:20.000", vtt, StringComparison.Ordinal);
            Assert.Contains("timeline-sprite.jpg#xywh=960,0,320,180", vtt, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }

            if (Directory.Exists(fakeFfTools.DirectoryPath))
            {
                Directory.Delete(fakeFfTools.DirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task HlsJob_StoresManifestAndProjectsPlaybackUrl()
    {
        var fakeFfTools = CreateFakeFfTools();
        try
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["WorkVideos:Hls:FfmpegPath"] = fakeFfTools.FfmpegPath,
                        ["WorkVideos:Hls:FfprobePath"] = fakeFfTools.FfprobePath,
                        ["WorkVideos:Hls:SegmentDurationSeconds"] = "4",
                        ["WorkVideos:Hls:TimelinePreviewIntervalSeconds"] = "5",
                        ["WorkVideos:Hls:TimelinePreviewTileColumns"] = "4"
                    });
                });
            });

            var client = await CreateAuthenticatedClientAsync(factory);
            var created = await CreateWorkAsync(client, $"HLS Work {Guid.NewGuid():N}");

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(SampleMp4Bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            form.Add(fileContent, "file", "demo.mp4");
            form.Add(new StringContent("0"), "expectedVideosVersion");

            var response = await client.PostAsync($"/api/admin/works/{created.Id}/videos/hls-job", form);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<MutationPayload>();

            Assert.NotNull(payload);
            var video = Assert.Single(payload!.Videos);
            Assert.Equal(WorkVideoSourceTypes.Hls, video.SourceType);
            Assert.NotNull(video.PlaybackUrl);
            Assert.EndsWith("/master.m3u8", video.PlaybackUrl, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("/timeline.vtt", video.TimelinePreviewVttUrl, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("/timeline-sprite.jpg", video.TimelinePreviewSpriteUrl, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(":videos/" + created.Id.ToString("N") + "/" + video.Id.ToString("N") + "/hls/master.m3u8", video.SourceKey, StringComparison.OrdinalIgnoreCase);
            Assert.False(video.SourceKey.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

            var manifestResponse = await client.GetAsync(video.PlaybackUrl);
            manifestResponse.EnsureSuccessStatusCode();
            var manifestBody = await manifestResponse.Content.ReadAsStringAsync();
            Assert.Contains("segment_00000.ts", manifestBody, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(fakeFfTools.DirectoryPath))
            {
                Directory.Delete(fakeFfTools.DirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task HlsJob_DeleteThenReupload_ReturnsPreviewCapablePayloadWhenPreviewAssetsExist()
    {
        var fakeFfTools = CreateFakeFfTools();
        try
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["WorkVideos:Hls:FfmpegPath"] = fakeFfTools.FfmpegPath,
                        ["WorkVideos:Hls:FfprobePath"] = fakeFfTools.FfprobePath,
                        ["WorkVideos:Hls:SegmentDurationSeconds"] = "4",
                        ["WorkVideos:Hls:TimelinePreviewIntervalSeconds"] = "5",
                        ["WorkVideos:Hls:TimelinePreviewTileColumns"] = "4"
                    });
                });
            });

            var client = await CreateAuthenticatedClientAsync(factory);
            var created = await CreateWorkAsync(client, $"HLS Reupload Work {Guid.NewGuid():N}");

            var firstUpload = await UploadHlsVideoAsync(client, created.Id, expectedVersion: 0);
            var firstVideo = Assert.Single(firstUpload.Videos);
            Assert.NotNull(firstVideo.TimelinePreviewVttUrl);
            Assert.NotNull(firstVideo.TimelinePreviewSpriteUrl);

            var deleteResponse = await client.DeleteAsync($"/api/admin/works/{created.Id}/videos/{firstVideo.Id}?expectedVideosVersion={firstUpload.VideosVersion}");
            deleteResponse.EnsureSuccessStatusCode();
            var deletePayload = await deleteResponse.Content.ReadFromJsonAsync<MutationPayload>();
            Assert.NotNull(deletePayload);
            Assert.Empty(deletePayload!.Videos);

            var secondUpload = await UploadHlsVideoAsync(client, created.Id, expectedVersion: deletePayload.VideosVersion);
            var secondVideo = Assert.Single(secondUpload.Videos);
            Assert.NotNull(secondVideo.TimelinePreviewVttUrl);
            Assert.NotNull(secondVideo.TimelinePreviewSpriteUrl);

            var publicResponse = await client.GetAsync($"/api/public/works/{created.Slug}");
            publicResponse.EnsureSuccessStatusCode();
            using var publicDocument = JsonDocument.Parse(await publicResponse.Content.ReadAsStringAsync());
            var publicVideo = publicDocument.RootElement.GetProperty("videos").EnumerateArray().Single();
            Assert.False(string.IsNullOrWhiteSpace(publicVideo.GetProperty("timeline_preview_vtt_url").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(publicVideo.GetProperty("timeline_preview_sprite_url").GetString()));
        }
        finally
        {
            if (Directory.Exists(fakeFfTools.DirectoryPath))
            {
                Directory.Delete(fakeFfTools.DirectoryPath, recursive: true);
            }
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
        var service = scope.ServiceProvider.GetRequiredService<IWorkVideoCleanupService>();
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

    private static async Task<MutationPayload> UploadHlsVideoAsync(HttpClient client, Guid workId, int expectedVersion)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(SampleMp4Bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        form.Add(fileContent, "file", "demo.mp4");
        form.Add(new StringContent(expectedVersion.ToString()), "expectedVideosVersion");

        var response = await client.PostAsync($"/api/admin/works/{workId}/videos/hls-job", form);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<MutationPayload>();
        return payload!;
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "admin");
        var csrfPayload = await client.GetFromJsonAsync<CsrfTokenResponse>("/api/auth/csrf");
        if (!string.IsNullOrWhiteSpace(csrfPayload?.RequestToken))
        {
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfPayload.RequestToken);
        }

        return client;
    }

    private static FakeFfTools CreateFakeFfTools()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"portfolio-tests-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var ffmpegPath = Path.Combine(directory, "ffmpeg");
        var ffprobePath = Path.Combine(directory, "ffprobe");
        var argsPath = Path.Combine(directory, "args.txt");
        var escapedArgsPath = QuoteShellValue(argsPath);

        File.WriteAllText(ffmpegPath, $$"""
#!/bin/sh
printf '%s\n' "$@" > {{escapedArgsPath}}
out=""
for arg in "$@"; do out="$arg"; done
dir=$(dirname "$out")
mkdir -p "$dir"
cat > "$out" <<'EOF'
#EXTM3U
#EXT-X-VERSION:3
#EXT-X-TARGETDURATION:1
#EXT-X-MEDIA-SEQUENCE:0
#EXTINF:1.0,
segment_00000.ts
#EXT-X-ENDLIST
EOF
printf 'segment' > "$dir/segment_00000.ts"
""");
        File.WriteAllText(ffprobePath, """
#!/bin/sh
printf '20.0'
""");

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                ffmpegPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.SetUnixFileMode(
                ffprobePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return new FakeFfTools(directory, ffmpegPath, ffprobePath, argsPath);
    }

    private static string QuoteShellValue(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
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
        public string SourceType { get; set; } = string.Empty;
        public string SourceKey { get; set; } = string.Empty;
        public string? PlaybackUrl { get; set; }
        [JsonPropertyName("timeline_preview_vtt_url")]
        public string? TimelinePreviewVttUrl { get; set; }
        [JsonPropertyName("timeline_preview_sprite_url")]
        public string? TimelinePreviewSpriteUrl { get; set; }
    }

    private sealed record CsrfTokenResponse(string RequestToken, string HeaderName);

    private sealed record FakeFfTools(string DirectoryPath, string FfmpegPath, string FfprobePath, string ArgsPath);

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
