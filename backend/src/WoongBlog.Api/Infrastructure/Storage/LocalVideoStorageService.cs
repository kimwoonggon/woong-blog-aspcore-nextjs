using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Infrastructure.Storage;

public sealed class LocalVideoStorageService(
    IOptions<AuthOptions> authOptions) : IVideoObjectStorage
{
    private readonly AuthOptions _authOptions = authOptions.Value;

    public string StorageType => WorkVideoSourceTypes.Local;

    public string? BuildPlaybackUrl(string storageKey)
    {
        return $"/media/{storageKey}";
    }

    public Task<VideoUploadTargetResult> CreateUploadTargetAsync(
        Guid workId,
        Guid uploadSessionId,
        string storageKey,
        string contentType,
        CancellationToken cancellationToken)
    {
        var uploadUrl = $"/api/admin/works/{workId}/videos/upload?uploadSessionId={uploadSessionId}";
        return Task.FromResult(new VideoUploadTargetResult(uploadSessionId, "POST", uploadUrl, storageKey));
    }

    public async Task SaveDirectUploadAsync(string storageKey, Stream stream, string contentType, CancellationToken cancellationToken)
    {
        var physicalPath = Path.Combine(_authOptions.MediaRoot, storageKey);
        var directory = Path.GetDirectoryName(physicalPath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The local video upload path could not be resolved.");
        }

        Directory.CreateDirectory(directory);
        await using var output = File.Create(physicalPath);
        await stream.CopyToAsync(output, cancellationToken);
    }

    public Task<VideoStoredObject?> GetObjectAsync(string storageKey, CancellationToken cancellationToken)
    {
        var physicalPath = Path.Combine(_authOptions.MediaRoot, storageKey);
        if (!File.Exists(physicalPath))
        {
            return Task.FromResult<VideoStoredObject?>(null);
        }

        var fileInfo = new FileInfo(physicalPath);
        return Task.FromResult<VideoStoredObject?>(new VideoStoredObject("video/mp4", fileInfo.Length));
    }

    public async Task<byte[]> ReadPrefixAsync(string storageKey, int length, CancellationToken cancellationToken)
    {
        var physicalPath = Path.Combine(_authOptions.MediaRoot, storageKey);
        if (!File.Exists(physicalPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(physicalPath);
        var buffer = new byte[length];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, length), cancellationToken);
        return buffer[..bytesRead];
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var physicalPath = Path.Combine(_authOptions.MediaRoot, storageKey);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }
}
