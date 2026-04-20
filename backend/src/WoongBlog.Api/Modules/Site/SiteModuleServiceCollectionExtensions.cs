using WoongBlog.Api.Modules.Site.Application.Abstractions;
using WoongBlog.Api.Modules.Site.Persistence;

namespace WoongBlog.Api.Modules.Site;

internal static class SiteModuleServiceCollectionExtensions
{
    public static IServiceCollection AddSiteModule(this IServiceCollection services)
    {
        services.AddScoped<ISiteSettingsCommandStore, SiteSettingsCommandStore>();
        services.AddScoped<ISiteSettingsQueryStore, SiteSettingsQueryStore>();
        return services;
    }
}
