using WoongBlog.Api.Modules.Composition.Application.GetHome;

namespace WoongBlog.Api.Modules.Composition.Application.Abstractions;

public interface IPublicHomeService
{
    Task<HomeDto?> GetHomeAsync(CancellationToken cancellationToken);
}
