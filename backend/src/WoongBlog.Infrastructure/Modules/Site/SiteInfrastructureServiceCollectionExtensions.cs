using WoongBlog.Application.Modules.Site.Abstractions;
using WoongBlog.Infrastructure.Modules.Site.Persistence;

namespace WoongBlog.Infrastructure.Modules.Site;

public static class SiteInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSiteInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ISiteSettingsCommandStore, SiteSettingsCommandStore>();
        services.AddScoped<ISiteSettingsQueryStore, SiteSettingsQueryStore>();
        return services;
    }
}
