namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public interface IWorkVideoStorageSelector
{
    string ResolveStorageType();
    bool TryGetStorage(string storageType, out IVideoObjectStorage storage);
}
