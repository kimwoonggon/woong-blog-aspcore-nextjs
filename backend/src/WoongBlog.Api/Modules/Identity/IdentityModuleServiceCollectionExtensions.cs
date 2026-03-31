using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Modules.Identity.Application;

namespace WoongBlog.Api.Modules.Identity;

internal static class IdentityModuleServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddAuthInfrastructure(configuration, environment);
        services.AddScoped<IIdentityInteractionService, IdentityInteractionService>();
        return services;
    }
}
