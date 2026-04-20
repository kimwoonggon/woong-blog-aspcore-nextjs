using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Blogs.Persistence;

namespace WoongBlog.Api.Modules.Content.Blogs;

internal static class BlogsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddBlogsModule(this IServiceCollection services)
    {
        services.AddScoped<IBlogCommandStore, BlogCommandStore>();
        services.AddScoped<IBlogQueryStore, BlogQueryStore>();
        return services;
    }
}
