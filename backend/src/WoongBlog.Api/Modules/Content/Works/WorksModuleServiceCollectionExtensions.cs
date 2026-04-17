namespace WoongBlog.Api.Modules.Content.Works;

internal static class WorksModuleServiceCollectionExtensions
{
    public static IServiceCollection AddWorksModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WoongBlog.Api.Infrastructure.Storage.CloudflareR2Options>()
            .Bind(configuration.GetSection(WoongBlog.Api.Infrastructure.Storage.CloudflareR2Options.SectionName));
        services.AddOptions<WoongBlog.Api.Modules.Content.Works.Application.WorkVideos.WorkVideoHlsOptions>()
            .Bind(configuration.GetSection(WoongBlog.Api.Modules.Content.Works.Application.WorkVideos.WorkVideoHlsOptions.SectionName));
        services.AddScoped<WoongBlog.Api.Modules.Content.Works.Application.WorkVideos.IWorkVideoService, WoongBlog.Api.Modules.Content.Works.Application.WorkVideos.WorkVideoService>();
        services.AddScoped<WoongBlog.Api.Modules.Content.Works.Application.WorkVideos.IVideoObjectStorage, WoongBlog.Api.Infrastructure.Storage.LocalVideoStorageService>();
        services.AddScoped<WoongBlog.Api.Modules.Content.Works.Application.WorkVideos.IVideoObjectStorage, WoongBlog.Api.Infrastructure.Storage.R2VideoStorageService>();
        services.AddScoped<WoongBlog.Api.Modules.Content.Works.Application.WorkVideos.IWorkVideoPlaybackUrlBuilder, WoongBlog.Api.Infrastructure.Storage.WorkVideoPlaybackUrlBuilder>();
        services.AddHostedService<WoongBlog.Api.Modules.Content.Works.Application.WorkVideos.VideoStorageCleanupWorker>();
        return services;
    }
}
