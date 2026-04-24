using WoongBlog.Infrastructure.Ai;
using WoongBlog.Application.Modules.AI.Abstractions;
using WoongBlog.Application.Modules.AI.BatchJobs;
using WoongBlog.Infrastructure.Modules.AI.Persistence;

namespace WoongBlog.Infrastructure.Modules.AI;

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
