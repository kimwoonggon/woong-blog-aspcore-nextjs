using WoongBlog.Application.Modules.Media.Abstractions;
using WoongBlog.Infrastructure.Modules.Media.Persistence;
using WoongBlog.Infrastructure.Modules.Media.Policies;
using WoongBlog.Infrastructure.Modules.Media.Storage;

namespace WoongBlog.Infrastructure.Modules.Media;

public static class MediaInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddMediaInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IMediaAssetCommandStore, MediaAssetCommandStore>();
        services.AddScoped<IMediaAssetStorage, MediaAssetStorage>();
        services.AddScoped<IMediaAssetUploadPolicy, MediaAssetUploadPolicy>();
        return services;
    }
}
