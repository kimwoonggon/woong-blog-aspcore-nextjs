using WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Pages.Persistence;

namespace WoongBlog.Api.Modules.Content.Pages;

public static class PagesInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPagesInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPageCommandStore, PageCommandStore>();
        services.AddScoped<IPageQueryStore, PageQueryStore>();
        return services;
    }
}
