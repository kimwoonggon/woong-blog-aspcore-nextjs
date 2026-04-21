using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Infrastructure.Proxy;
using WoongBlog.Api.Infrastructure.Security;

namespace WoongBlog.Api.Infrastructure;

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
