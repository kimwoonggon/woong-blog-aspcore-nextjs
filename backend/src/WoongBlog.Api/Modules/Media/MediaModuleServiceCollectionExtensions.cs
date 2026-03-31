using WoongBlog.Api.Modules.Media.Application;

namespace WoongBlog.Api.Modules.Media;

internal static class MediaModuleServiceCollectionExtensions
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services)
    {
        services.AddScoped<IMediaAssetService, MediaAssetService>();
        return services;
    }
}
