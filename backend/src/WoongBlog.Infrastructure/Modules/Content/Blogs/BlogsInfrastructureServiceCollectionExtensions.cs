using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Blogs.Persistence;

namespace WoongBlog.Api.Modules.Content.Blogs;

public static class BlogsInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBlogsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IBlogCommandStore, BlogCommandStore>();
        services.AddScoped<IBlogQueryStore, BlogQueryStore>();
        return services;
    }
}
