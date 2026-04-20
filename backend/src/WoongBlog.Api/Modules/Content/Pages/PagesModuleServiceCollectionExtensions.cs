using WoongBlog.Api.Modules.Content.Pages.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Pages.Persistence;

namespace WoongBlog.Api.Modules.Content.Pages;

internal static class PagesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddPagesModule(this IServiceCollection services)
    {
        services.AddScoped<IPageCommandStore, PageCommandStore>();
        services.AddScoped<IPageQueryStore, PageQueryStore>();
        return services;
    }
}
