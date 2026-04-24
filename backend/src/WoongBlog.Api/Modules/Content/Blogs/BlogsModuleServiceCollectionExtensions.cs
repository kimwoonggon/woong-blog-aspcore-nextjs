using WoongBlog.Infrastructure.Modules.Content.Blogs;

namespace WoongBlog.Api.Modules.Content.Blogs;

internal static class BlogsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddBlogsModule(this IServiceCollection services)
    {
        return services.AddBlogsInfrastructure();
    }
}
