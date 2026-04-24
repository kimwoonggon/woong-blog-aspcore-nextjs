using WoongBlog.Infrastructure.Modules.Site;

namespace WoongBlog.Api.Modules.Site;

internal static class SiteModuleServiceCollectionExtensions
{
    public static IServiceCollection AddSiteModule(this IServiceCollection services)
    {
        return services.AddSiteInfrastructure();
    }
}
