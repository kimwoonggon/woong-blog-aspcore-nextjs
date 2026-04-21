using WoongBlog.Api.Common.Application.Files;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed class WorkVideoHlsWorkspace : IWorkVideoHlsWorkspace
{
    public async Task<WorkVideoHlsWorkspaceLease> CreateAsync(
        IUploadedFile file,
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
