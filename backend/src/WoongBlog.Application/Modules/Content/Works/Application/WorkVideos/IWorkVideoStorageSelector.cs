namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoStorageSelector
{
    string ResolveStorageType();
    bool TryGetStorage(string storageType, out IVideoObjectStorage storage);
}
