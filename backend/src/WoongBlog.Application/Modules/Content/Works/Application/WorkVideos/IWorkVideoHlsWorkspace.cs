using WoongBlog.Api.Common.Application.Files;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoHlsWorkspace
{
    Task<WorkVideoHlsWorkspaceLease> CreateAsync(
        IUploadedFile file,
        Guid videoId,
        CancellationToken cancellationToken);
}

public sealed class WorkVideoHlsWorkspaceLease(
    string tempDirectory,
    string sourcePath,
    string hlsDirectory) : IAsyncDisposable
{
    public string TempDirectory { get; } = tempDirectory;
    public string SourcePath { get; } = sourcePath;
    public string HlsDirectory { get; } = hlsDirectory;

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(TempDirectory))
        {
            Directory.Delete(TempDirectory, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
