using WoongBlog.Api.Common.Application.Files;

namespace WoongBlog.Api.Modules.Media.Application.Abstractions;

public interface IMediaAssetStorage
{
    Task SaveAsync(string relativePath, IUploadedFile file, CancellationToken cancellationToken);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}
