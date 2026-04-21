using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.BatchJobs;

namespace WoongBlog.Api.Modules.AI;

internal static class AiModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAiModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAiModuleInfrastructure(configuration);
        services.AddScoped<IAiBatchJobScheduler, AiBatchJobScheduler>();
        services.AddScoped<IAiBatchJobRunner, AiBatchJobRunner>();
        services.AddScoped<IAiBatchJobItemProcessor, AiBatchJobItemProcessor>();
        services.AddSingleton<IBlogFixApplyPolicy, BlogFixApplyPolicy>();
        return services;
    }
}
