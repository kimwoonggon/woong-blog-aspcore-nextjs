using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Infrastructure.Storage;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

public sealed class WorkVideoStorageSelector(
    IEnumerable<IVideoObjectStorage> storages,
    IHostEnvironment environment,
    IOptions<CloudflareR2Options> r2Options) : IWorkVideoStorageSelector
{
    private readonly IHostEnvironment _environment = environment;
    private readonly CloudflareR2Options _r2Options = r2Options.Value;
    private readonly IReadOnlyDictionary<string, IVideoObjectStorage> _storages = storages
        .ToDictionary(storage => storage.StorageType, StringComparer.OrdinalIgnoreCase);

    public string ResolveStorageType()
    {
        if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
        {
            if (!_r2Options.ForceEnabledInDevelopment)
            {
                return WorkVideoSourceTypes.Local;
            }
        }

        return _storages.ContainsKey(WorkVideoSourceTypes.R2)
            && _storages[WorkVideoSourceTypes.R2].BuildPlaybackUrl("health-check") is not null
            ? WorkVideoSourceTypes.R2
            : WorkVideoSourceTypes.Local;
    }

    public bool TryGetStorage(string storageType, out IVideoObjectStorage storage)
    {
        return _storages.TryGetValue(storageType, out storage!);
    }
}
