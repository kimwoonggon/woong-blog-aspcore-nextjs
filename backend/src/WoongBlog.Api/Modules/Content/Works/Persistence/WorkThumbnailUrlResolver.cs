using System.Text.Json;
using System.Text.RegularExpressions;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Works.Persistence;

internal static partial class WorkThumbnailUrlResolver
{
    public static string ResolveThumbnailUrl(
        Guid? thumbnailAssetId,
        string? contentJson,
        IReadOnlyList<WorkVideo> videos,
        IReadOnlyDictionary<Guid, string> assetUrls)
    {
        if (thumbnailAssetId is not null && assetUrls.TryGetValue(thumbnailAssetId.Value, out var explicitUrl))
        {
            return explicitUrl;
        }

        var preferredVideo = SelectPreferredVideo(videos);
        if (preferredVideo is not null)
        {
            if (string.Equals(preferredVideo.SourceType, "youtube", StringComparison.OrdinalIgnoreCase))
            {
                return $"https://img.youtube.com/vi/{preferredVideo.SourceKey}/hqdefault.jpg";
            }

            return string.Empty;
        }

        return ExtractFirstContentImageUrl(contentJson) ?? string.Empty;
    }

    private static WorkVideo? SelectPreferredVideo(IReadOnlyList<WorkVideo> videos)
    {
        var orderedVideos = videos
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        return orderedVideos.FirstOrDefault(x => !string.Equals(x.SourceType, "youtube", StringComparison.OrdinalIgnoreCase))
            ?? orderedVideos.FirstOrDefault();
    }

    private static string? ExtractFirstContentImageUrl(string? contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(contentJson);
            if (!document.RootElement.TryGetProperty("html", out var htmlElement) || htmlElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var html = htmlElement.GetString();
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var match = FirstImageSrcRegex().Match(html);
            return match.Success ? match.Groups[2].Value : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex("<img\\b[^>]*\\bsrc=(\"|')(.*?)\\1", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FirstImageSrcRegex();
}
