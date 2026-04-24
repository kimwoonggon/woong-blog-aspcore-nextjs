using WoongBlog.Application.Modules.Composition.Abstractions;
using WoongBlog.Infrastructure.Modules.Composition.Persistence;

namespace WoongBlog.Infrastructure.Modules.Composition;

public static class CompositionInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCompositionInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAdminDashboardQueryStore, AdminDashboardQueryStore>();
        services.AddScoped<IHomeQueryStore, HomeQueryStore>();
        return services;
    }
}
