using WoongBlog.Api.Modules.Site.Application.Abstractions;
using WoongBlog.Api.Modules.Site.Persistence;

namespace WoongBlog.Api.Modules.Site;

public static class SiteInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSiteInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ISiteSettingsCommandStore, SiteSettingsCommandStore>();
        services.AddScoped<ISiteSettingsQueryStore, SiteSettingsQueryStore>();
        return services;
    }
}
