namespace WoongBlog.Api.Modules.Identity;

internal static class IdentityModuleServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        return services.AddIdentityInfrastructure(configuration, environment);
    }
}
