namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public interface IWorkVideoFileInspector
{
    Task<bool> LooksLikeMp4Async(string filePath, CancellationToken cancellationToken);

    Task<bool> LooksLikeMp4Async(
        string storageKey,
        IVideoObjectStorage storage,
        CancellationToken cancellationToken);
}
