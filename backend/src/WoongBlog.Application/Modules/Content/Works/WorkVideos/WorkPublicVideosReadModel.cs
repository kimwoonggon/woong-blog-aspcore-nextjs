using System.Text.Json;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public static class WorkPublicVideosReadModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(IEnumerable<WorkVideo> videos)
    {
        var rows = videos
            .OrderBy(video => video.SortOrder)
            .ThenBy(video => video.CreatedAt)
            .Select(video => new WorkPublicVideoSnapshot(
                video.Id,
                video.SourceType,
                video.SourceKey,
                video.OriginalFileName,
                video.MimeType,
                video.FileSize,
                video.Width,
                video.Height,
                video.DurationSeconds,
                video.TimelinePreviewVttStorageKey,
                video.TimelinePreviewSpriteStorageKey,
                video.SortOrder,
                video.CreatedAt))
            .ToArray();

        return JsonSerializer.Serialize(rows, JsonOptions);
    }

    public static IReadOnlyList<WorkPublicVideoSnapshot> Deserialize(string? publicVideosJson)
    {
        if (string.IsNullOrWhiteSpace(publicVideosJson))
        {
            return Array.Empty<WorkPublicVideoSnapshot>();
        }

        try
        {
            return JsonSerializer.Deserialize<WorkPublicVideoSnapshot[]>(publicVideosJson, JsonOptions)
                ?? Array.Empty<WorkPublicVideoSnapshot>();
        }
        catch (JsonException)
        {
            return Array.Empty<WorkPublicVideoSnapshot>();
        }
    }

    public static void Refresh(Work work, IEnumerable<WorkVideo> videos)
    {
        work.PublicVideosJson = Serialize(videos);
    }
}

public sealed record WorkPublicVideoSnapshot(
    Guid Id,
    string SourceType,
    string SourceKey,
    string? OriginalFileName,
    string? MimeType,
    long? FileSize,
    int? Width,
    int? Height,
    double? DurationSeconds,
    string? TimelinePreviewVttStorageKey,
    string? TimelinePreviewSpriteStorageKey,
    int SortOrder,
    DateTimeOffset CreatedAt);
