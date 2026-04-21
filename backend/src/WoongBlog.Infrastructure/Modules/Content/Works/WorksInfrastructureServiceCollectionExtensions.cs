using WoongBlog.Api.Infrastructure.Storage;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;
using WoongBlog.Api.Modules.Content.Works.Persistence;

namespace WoongBlog.Api.Modules.Content.Works;

public static class WorksInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddWorksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IWorkCommandStore, WorkCommandStore>();
        services.AddScoped<IWorkQueryStore, WorkQueryStore>();
        services.AddScoped<IWorkVideoCommandStore, WorkVideoCommandStore>();
        services.AddScoped<IWorkVideoCleanupStore, WorkVideoCleanupStore>();
        services.AddScoped<IWorkVideoQueryStore, WorkVideoQueryStore>();
        services.AddOptions<CloudflareR2Options>()
            .Bind(configuration.GetSection(CloudflareR2Options.SectionName));
        services.AddOptions<WorkVideoHlsOptions>()
            .Bind(configuration.GetSection(WorkVideoHlsOptions.SectionName));
        services.AddScoped<IWorkVideoCleanupService, WorkVideoService>();
        services.AddScoped<IVideoObjectStorage, LocalVideoStorageService>();
        services.AddScoped<IVideoObjectStorage, R2VideoStorageService>();
        services.AddScoped<IWorkVideoStorageSelector, WorkVideoStorageSelector>();
        services.AddScoped<IWorkVideoFileInspector, WorkVideoFileInspector>();
        services.AddScoped<IWorkVideoHlsWorkspace, WorkVideoHlsWorkspace>();
        services.AddScoped<IWorkVideoHlsOutputPublisher, WorkVideoHlsOutputPublisher>();
        services.AddScoped<IVideoTranscoder, FfmpegVideoTranscoder>();
        services.AddScoped<IWorkVideoPlaybackUrlBuilder, WorkVideoPlaybackUrlBuilder>();
        services.AddHostedService<VideoStorageCleanupWorker>();
        return services;
    }
}
