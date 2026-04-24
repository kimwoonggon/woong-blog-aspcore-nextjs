using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Infrastructure.Proxy;
using WoongBlog.Infrastructure.Security;

namespace WoongBlog.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddProxyInfrastructure(configuration);
        services.AddSecurityInfrastructure(configuration, environment);
        services.AddPersistenceInfrastructure(configuration);

        return services;
    }
}
