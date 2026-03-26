using Portfolio.Api.Application.Public.GetHome;

namespace Portfolio.Api.Application.Public.Abstractions;

public interface IPublicHomeService
{
    Task<HomeDto?> GetHomeAsync(CancellationToken cancellationToken);
}
