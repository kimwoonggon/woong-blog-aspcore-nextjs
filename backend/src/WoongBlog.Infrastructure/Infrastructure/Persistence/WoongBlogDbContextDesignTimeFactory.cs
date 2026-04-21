using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WoongBlog.Api.Infrastructure.Persistence;

public sealed class WoongBlogDbContextDesignTimeFactory : IDesignTimeDbContextFactory<WoongBlogDbContext>
{
    public WoongBlogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:Postgres")
            ?? "Host=localhost;Port=5432;Database=portfolio;Username=portfolio;Password=portfolio";

        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(WoongBlogDbContext).Assembly.FullName))
            .Options;

        return new WoongBlogDbContext(options);
    }
}
