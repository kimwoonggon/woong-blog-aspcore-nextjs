using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.AI.Application.BatchJobs;
using WoongBlog.Api.Modules.AI.Persistence;

namespace WoongBlog.Api.Modules.AI;

internal static class AiModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAiModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAiInfrastructure(configuration);
        services.AddScoped<IAiBlogFixBatchStore, AiBlogFixBatchStore>();
        services.AddScoped<IAiBatchJobRunner, AiBatchJobRunner>();
        services.AddScoped<IAiBatchJobItemProcessor, AiBatchJobItemProcessor>();
        services.AddScoped<IBlogFixApplyPolicy, BlogFixApplyPolicy>();
        return services;
    }
}
