using WoongBlog.Application.Modules.Content.Pages.Abstractions;
using WoongBlog.Infrastructure.Modules.Content.Pages.Persistence;

namespace WoongBlog.Infrastructure.Modules.Content.Pages;

public static class PagesInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPagesInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPageCommandStore, PageCommandStore>();
        services.AddScoped<IPageQueryStore, PageQueryStore>();
        return services;
    }
}
