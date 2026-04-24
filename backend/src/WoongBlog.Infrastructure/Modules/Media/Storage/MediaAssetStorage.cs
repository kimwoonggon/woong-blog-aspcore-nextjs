using Microsoft.Extensions.Options;
using WoongBlog.Infrastructure.Auth;
using WoongBlog.Api.Common.Application.Files;
using WoongBlog.Application.Modules.Media.Abstractions;

namespace WoongBlog.Infrastructure.Modules.Media.Storage;

public sealed class MediaAssetStorage(IOptions<AuthOptions> authOptions) : IMediaAssetStorage
{
    private readonly string _mediaRoot = authOptions.Value.MediaRoot;

    public async Task SaveAsync(string relativePath, IUploadedFile file, CancellationToken cancellationToken)
    {
        var physicalPath = Path.Combine(_mediaRoot, relativePath);
        var directory = Path.GetDirectoryName(physicalPath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The upload path could not be resolved.");
        }

        Directory.CreateDirectory(directory);

        await using var stream = File.Create(physicalPath);
        await using var uploadStream = file.OpenReadStream();
        await uploadStream.CopyToAsync(stream, cancellationToken);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        var physicalPath = Path.Combine(_mediaRoot, relativePath);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }
}
