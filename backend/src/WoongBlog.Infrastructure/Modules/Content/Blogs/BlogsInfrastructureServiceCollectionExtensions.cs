using WoongBlog.Application.Modules.Content.Blogs.Abstractions;
using WoongBlog.Infrastructure.Modules.Content.Blogs.Persistence;

namespace WoongBlog.Infrastructure.Modules.Content.Blogs;

public static class BlogsInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBlogsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IBlogCommandStore, BlogCommandStore>();
        services.AddScoped<IBlogQueryStore, BlogQueryStore>();
        return services;
    }
}
