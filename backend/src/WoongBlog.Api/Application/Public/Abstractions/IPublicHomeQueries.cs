using WoongBlog.Api.Application.Public.GetHome;

namespace WoongBlog.Api.Application.Public.Abstractions;

public interface IPublicHomeQueries
{
    Task<HomeDto?> GetHomeAsync(CancellationToken cancellationToken);
}
