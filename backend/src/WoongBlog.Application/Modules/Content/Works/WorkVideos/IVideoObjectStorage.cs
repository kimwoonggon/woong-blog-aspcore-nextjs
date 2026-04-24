namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public interface IVideoObjectStorage
{
    string StorageType { get; }
    string? BuildPlaybackUrl(string storageKey);
    Task<VideoUploadTargetResult> CreateUploadTargetAsync(
        Guid workId,
        Guid uploadSessionId,
        string storageKey,
        string contentType,
        CancellationToken cancellationToken);
    Task SaveDirectUploadAsync(string storageKey, Stream stream, string contentType, CancellationToken cancellationToken);
    Task<VideoStoredObject?> GetObjectAsync(string storageKey, CancellationToken cancellationToken);
    Task<byte[]> ReadPrefixAsync(string storageKey, int length, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

public sealed record VideoStoredObject(string? ContentType, long Size);
