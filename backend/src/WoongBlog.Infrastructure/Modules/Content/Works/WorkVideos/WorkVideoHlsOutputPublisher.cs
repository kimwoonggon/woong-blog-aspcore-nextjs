using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

public sealed class WorkVideoHlsOutputPublisher : IWorkVideoHlsOutputPublisher
{
    public async Task PublishAsync(
        IVideoObjectStorage storage,
        string hlsDirectory,
        string hlsPrefix,
        CancellationToken cancellationToken)
    {
        foreach (var filePath in Directory.EnumerateFiles(hlsDirectory).Order(StringComparer.Ordinal))
        {
            var fileName = Path.GetFileName(filePath);
            var storageKey = $"{hlsPrefix}/{fileName}";
            var contentType = fileName.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
                ? WorkVideoPolicy.HlsManifestContentType
                : fileName.EndsWith(".vtt", StringComparison.OrdinalIgnoreCase)
                    ? WorkVideoPolicy.TimelinePreviewVttContentType
                    : fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                        ? WorkVideoPolicy.TimelinePreviewSpriteContentType
                        : WorkVideoPolicy.HlsSegmentContentType;

            await using var stream = File.OpenRead(filePath);
            await storage.SaveDirectUploadAsync(storageKey, stream, contentType, cancellationToken);
        }
    }
}
