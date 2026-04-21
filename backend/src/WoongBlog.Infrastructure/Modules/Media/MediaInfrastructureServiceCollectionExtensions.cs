using WoongBlog.Api.Modules.Media.Application.Abstractions;
using WoongBlog.Api.Modules.Media.Persistence;
using WoongBlog.Api.Modules.Media.Policies;
using WoongBlog.Api.Modules.Media.Storage;

namespace WoongBlog.Api.Modules.Media;

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
