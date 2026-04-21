namespace WoongBlog.Api.Modules.Content.Pages;

internal static class PagesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddPagesModule(this IServiceCollection services)
    {
        return services.AddPagesInfrastructure();
    }
}
