using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.AI.Application.BatchJobs;
using WoongBlog.Api.Modules.AI.Persistence;

namespace WoongBlog.Api.Modules.AI;

public static class AiInfrastructureModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAiModuleInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAiInfrastructure(configuration);
        services.AddScoped<AiBlogFixBatchStore>();
        services.AddScoped<IAiBatchTargetQueryStore>(serviceProvider => serviceProvider.GetRequiredService<AiBlogFixBatchStore>());
        services.AddScoped<IAiBatchJobQueryStore>(serviceProvider => serviceProvider.GetRequiredService<AiBlogFixBatchStore>());
        services.AddScoped<IAiBatchJobCommandStore>(serviceProvider => serviceProvider.GetRequiredService<AiBlogFixBatchStore>());
        services.AddSingleton<IAiBatchJobItemDispatcher, AiBatchJobItemDispatcher>();
        return services;
    }
}
