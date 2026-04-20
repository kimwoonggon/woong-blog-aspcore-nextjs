using Microsoft.AspNetCore.Http;

namespace WoongBlog.Api.Modules.Media.Application.Abstractions;

public interface IMediaAssetStorage
{
    Task SaveAsync(string relativePath, IFormFile file, CancellationToken cancellationToken);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}
