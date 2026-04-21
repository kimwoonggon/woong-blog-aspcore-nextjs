namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoHlsOutputPublisher
{
    Task PublishAsync(
        IVideoObjectStorage storage,
        string hlsDirectory,
        string hlsPrefix,
        CancellationToken cancellationToken);
}
