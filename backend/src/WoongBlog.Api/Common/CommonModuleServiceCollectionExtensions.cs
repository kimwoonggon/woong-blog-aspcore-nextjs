using WoongBlog.Api.Application;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Infrastructure.Proxy;
using WoongBlog.Api.Infrastructure.Security;

namespace WoongBlog.Api.Common;

internal static class CommonModuleServiceCollectionExtensions
{
    public static IServiceCollection AddCommonModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddApiCore();
        services.AddProxyInfrastructure(configuration);
        services.AddSecurityInfrastructure(configuration, environment);
        services.AddPersistenceInfrastructure(configuration);
        return services;
    }
}
