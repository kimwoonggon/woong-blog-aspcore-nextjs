using System.Text.RegularExpressions;

namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public static class WorkVideoPolicy
{
    public const int MaxVideosPerWork = 10;
    public const long MaxVideoBytes = 200L * 1024L * 1024L;
    public const string HlsManifestFileName = "master.m3u8";
    public const string HlsManifestContentType = "application/vnd.apple.mpegurl";
    public const string HlsSegmentContentType = "video/mp2t";
    public const string TimelinePreviewVttFileName = "timeline.vtt";
    public const string TimelinePreviewVttContentType = "text/vtt";
    public const string TimelinePreviewSpriteFileName = "timeline-sprite.jpg";
    public const string TimelinePreviewSpriteContentType = "image/jpeg";

    private static readonly string[] AllowedMimeTypes = ["video/mp4"];
    private static readonly string[] AllowedExtensions = [".mp4"];

    public static string? ValidateVideoFile(string fileName, string contentType, long size)
    {
        if (size <= 0)
        {
            return "Video file size must be greater than zero.";
        }

        if (size > MaxVideoBytes)
        {
            return "Video file size must be 200MB or smaller.";
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return "Only .mp4 uploads are supported.";
        }

        if (!AllowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return "Only video/mp4 uploads are supported.";
        }

        return null;
    }

    public static bool LooksLikeMp4(byte[] prefix)
    {
        if (prefix.Length < 12)
        {
            return false;
        }

        for (var index = 4; index <= prefix.Length - 4; index += 1)
        {
            if (prefix[index] == (byte)'f'
                && prefix[index + 1] == (byte)'t'
                && prefix[index + 2] == (byte)'y'
                && prefix[index + 3] == (byte)'p')
            {
                return true;
            }
        }

        return false;
    }

    public static string? NormalizeYouTubeVideoId(string rawValue)
    {
        var trimmed = rawValue.Trim();
        if (Regex.IsMatch(trimmed, "^[A-Za-z0-9_-]{11}$"))
        {
            return trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host is "youtu.be")
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 && Regex.IsMatch(segments[0], "^[A-Za-z0-9_-]{11}$") ? segments[0] : null;
        }

        if (host is not "www.youtube.com" and not "youtube.com" and not "m.youtube.com")
        {
            return null;
        }

        if (uri.AbsolutePath.StartsWith("/watch", StringComparison.OrdinalIgnoreCase))
        {
            var videoId = GetQueryValue(uri.Query, "v");
            return !string.IsNullOrWhiteSpace(videoId) && Regex.IsMatch(videoId, "^[A-Za-z0-9_-]{11}$") ? videoId : null;
        }

        var pathSegments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length == 2 && (pathSegments[0] == "embed" || pathSegments[0] == "shorts"))
        {
            return Regex.IsMatch(pathSegments[1], "^[A-Za-z0-9_-]{11}$") ? pathSegments[1] : null;
        }

        return null;
    }

    private static string? GetQueryValue(string query, string key)
    {
        var trimmedQuery = query.TrimStart('?');
        foreach (var parameter in trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split('=', 2);
            if (parts.Length == 0 || !string.Equals(Uri.UnescapeDataString(parts[0]), key, StringComparison.Ordinal))
            {
                continue;
            }

            return parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
        }

        return null;
    }

    public static string SanitizeOriginalFileName(string fileName)
    {
        var sanitized = Path.GetFileName(fileName).Trim();
        return sanitized.Length <= 120 ? sanitized : sanitized[..120];
    }
}
