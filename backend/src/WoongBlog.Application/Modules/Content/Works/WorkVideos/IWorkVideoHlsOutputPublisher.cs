namespace WoongBlog.Application.Modules.Content.Works.WorkVideos;

public interface IWorkVideoHlsOutputPublisher
{
    Task PublishAsync(
        IVideoObjectStorage storage,
        string hlsDirectory,
        string hlsPrefix,
        CancellationToken cancellationToken);
}
