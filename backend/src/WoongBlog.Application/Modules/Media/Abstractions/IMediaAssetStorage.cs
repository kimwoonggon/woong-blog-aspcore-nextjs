using WoongBlog.Api.Common.Application.Files;

namespace WoongBlog.Application.Modules.Media.Abstractions;

public interface IMediaAssetStorage
{
    Task SaveAsync(string relativePath, IUploadedFile file, CancellationToken cancellationToken);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}
