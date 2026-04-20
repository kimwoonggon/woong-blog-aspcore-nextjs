namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoFileInspector
{
    Task<bool> LooksLikeMp4Async(string filePath, CancellationToken cancellationToken);

    Task<bool> LooksLikeMp4Async(
        string storageKey,
        IVideoObjectStorage storage,
        CancellationToken cancellationToken);
}

public sealed class WorkVideoFileInspector : IWorkVideoFileInspector
{
    private const int PrefixLength = 64;

    public async Task<bool> LooksLikeMp4Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var prefix = await ReadPrefixAsync(stream, cancellationToken);
        return WorkVideoPolicy.LooksLikeMp4(prefix);
    }

    public async Task<bool> LooksLikeMp4Async(
        string storageKey,
        IVideoObjectStorage storage,
        CancellationToken cancellationToken)
    {
        var prefix = await storage.ReadPrefixAsync(storageKey, PrefixLength, cancellationToken);
        return WorkVideoPolicy.LooksLikeMp4(prefix);
    }

    private static async Task<byte[]> ReadPrefixAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[PrefixLength];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, PrefixLength), cancellationToken);
        return buffer[..bytesRead];
    }
}
