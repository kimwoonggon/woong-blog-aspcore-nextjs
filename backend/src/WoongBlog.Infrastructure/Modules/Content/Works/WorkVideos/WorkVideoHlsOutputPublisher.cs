using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

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
                : WorkVideoPolicy.HlsSegmentContentType;

            await using var stream = File.OpenRead(filePath);
            await storage.SaveDirectUploadAsync(storageKey, stream, contentType, cancellationToken);
        }
    }
}
