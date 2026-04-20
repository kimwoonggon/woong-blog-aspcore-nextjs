using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public interface IWorkVideoHlsWorkspace
{
    Task<WorkVideoHlsWorkspaceLease> CreateAsync(
        IFormFile file,
        Guid videoId,
        CancellationToken cancellationToken);
}

public sealed class WorkVideoHlsWorkspace : IWorkVideoHlsWorkspace
{
    public async Task<WorkVideoHlsWorkspaceLease> CreateAsync(
        IFormFile file,
        Guid videoId,
        CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "woong-blog-hls", videoId.ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var sourcePath = Path.Combine(tempDirectory, "source.mp4");
        await using (var inputFile = File.Create(sourcePath))
        await using (var uploadStream = file.OpenReadStream())
        {
            await uploadStream.CopyToAsync(inputFile, cancellationToken);
        }

        var hlsDirectory = Path.Combine(tempDirectory, "hls");
        Directory.CreateDirectory(hlsDirectory);

        return new WorkVideoHlsWorkspaceLease(tempDirectory, sourcePath, hlsDirectory);
    }
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
