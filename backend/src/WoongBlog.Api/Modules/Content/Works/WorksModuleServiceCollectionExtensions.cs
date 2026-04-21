namespace WoongBlog.Api.Modules.Content.Works;

internal static class WorksModuleServiceCollectionExtensions
{
    public static IServiceCollection AddWorksModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddWorksInfrastructure(configuration);
    }
}
