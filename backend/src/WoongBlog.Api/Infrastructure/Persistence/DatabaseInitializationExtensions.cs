namespace WoongBlog.Api.Infrastructure.Persistence;

internal static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        await DatabaseBootstrapper.InitializeAsync(dbContext);
    }
}
