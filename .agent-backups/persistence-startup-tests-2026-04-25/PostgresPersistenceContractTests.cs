using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using WoongBlog.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

public sealed class PostgresPersistenceContractTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("portfolio")
        .WithUsername("portfolio")
        .WithPassword("portfolio")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Bootstrapper_AppliesPostgresSpecificSearchSchema()
    {
        await using var dbContext = CreateDbContext();

        await DatabaseBootstrapper.InitializeAsync(dbContext, TestContext.Current.CancellationToken);

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm')",
            TestContext.Current.CancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Blogs' AND column_name = 'SearchTitle')",
            TestContext.Current.CancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Works' AND column_name = 'SearchText')",
            TestContext.Current.CancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Blogs_SearchTitle_Trgm')",
            TestContext.Current.CancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Works_SearchText_Trgm')",
            TestContext.Current.CancellationToken));
        Assert.True(await ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM \"SchemaPatches\" WHERE \"Id\" = '20260419_content_search_fields')",
            TestContext.Current.CancellationToken));
    }

    private WoongBlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WoongBlogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new WoongBlogDbContext(options);
    }

    private static async Task<bool> ExistsAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }
}
