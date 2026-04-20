using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;
using WoongBlog.Api.Modules.Content.Works.Persistence;
using WoongBlog.Api.Infrastructure.Storage;

namespace WoongBlog.Api.Modules.Content.Works;

internal static class WorksModuleServiceCollectionExtensions
{
    public static IServiceCollection AddWorksModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IWorkCommandStore, WorkCommandStore>();
        services.AddScoped<IWorkQueryStore, WorkQueryStore>();
        services.AddOptions<CloudflareR2Options>()
            .Bind(configuration.GetSection(CloudflareR2Options.SectionName));
        services.AddOptions<WorkVideoHlsOptions>()
            .Bind(configuration.GetSection(WorkVideoHlsOptions.SectionName));
        services.AddScoped<IWorkVideoService, WorkVideoService>();
        services.AddScoped<IVideoObjectStorage, LocalVideoStorageService>();
        services.AddScoped<IVideoObjectStorage, R2VideoStorageService>();
        services.AddScoped<IWorkVideoPlaybackUrlBuilder, WorkVideoPlaybackUrlBuilder>();
        services.AddHostedService<VideoStorageCleanupWorker>();
        return services;
    }
}
