using WoongBlog.Api.Modules.Composition.Application.Abstractions;
using WoongBlog.Api.Modules.Composition.Persistence;

namespace WoongBlog.Api.Modules.Composition;

internal static class CompositionModuleServiceCollectionExtensions
{
    public static IServiceCollection AddCompositionModule(this IServiceCollection services)
    {
        services.AddScoped<IAdminDashboardQueryStore, AdminDashboardQueryStore>();
        services.AddScoped<IHomeQueryStore, HomeQueryStore>();
        return services;
    }
}
