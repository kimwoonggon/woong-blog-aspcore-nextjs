using WoongBlog.Api.Application.Public.GetHome;

namespace WoongBlog.Api.Application.Public.Abstractions;

public interface IPublicHomeService
{
    Task<HomeDto?> GetHomeAsync(CancellationToken cancellationToken);
}
