namespace WoongBlog.Api.Modules.Media;

internal static class MediaModuleServiceCollectionExtensions
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services)
    {
        return services.AddMediaInfrastructure();
    }
}
