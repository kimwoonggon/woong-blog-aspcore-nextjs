using WoongBlog.Api.Modules.Composition.Application.Abstractions;
using WoongBlog.Api.Modules.Composition.Persistence;

namespace WoongBlog.Api.Modules.Composition;

public static class CompositionInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCompositionInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAdminDashboardQueryStore, AdminDashboardQueryStore>();
        services.AddScoped<IHomeQueryStore, HomeQueryStore>();
        return services;
    }
}
