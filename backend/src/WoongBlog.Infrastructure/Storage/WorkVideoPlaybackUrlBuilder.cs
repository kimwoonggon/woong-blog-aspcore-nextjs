using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Infrastructure.Storage;

public sealed class WorkVideoPlaybackUrlBuilder(
    IEnumerable<IVideoObjectStorage> storages) : IWorkVideoPlaybackUrlBuilder
{
    private readonly IReadOnlyDictionary<string, IVideoObjectStorage> _storages = storages
        .ToDictionary(storage => storage.StorageType, StringComparer.OrdinalIgnoreCase);

    public string? BuildPlaybackUrl(string sourceType, string sourceKey)
    {
        if (string.Equals(sourceType, WorkVideoSourceTypes.YouTube, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(sourceType, WorkVideoSourceTypes.Hls, StringComparison.OrdinalIgnoreCase))
        {
            return WorkVideoHlsSourceKey.TryParse(sourceKey, out var storageType, out var manifestStorageKey)
                && _storages.TryGetValue(storageType, out var hlsStorage)
                    ? hlsStorage.BuildPlaybackUrl(manifestStorageKey)
                    : null;
        }

        return _storages.TryGetValue(sourceType, out var storage)
            ? storage.BuildPlaybackUrl(sourceKey)
            : null;
    }

    public string? BuildStorageObjectUrl(string storageType, string storageKey)
    {
        return _storages.TryGetValue(storageType, out var storage)
            ? storage.BuildPlaybackUrl(storageKey)
            : null;
    }
}
