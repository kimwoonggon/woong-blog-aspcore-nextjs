using Microsoft.Extensions.Configuration;
using WoongBlog.Infrastructure.Persistence.Diagnostics;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public sealed class PersistenceRuntimeDiagnosticsComponentTests
{
    [Fact]
    public void FromConfiguration_ExposesPostgresPoolSettingsWithoutSecrets()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Postgres",
                ["ConnectionStrings:Postgres"] = "Host=db;Port=5432;Database=portfolio;Username=portfolio;Password=secret;Maximum Pool Size=37;Minimum Pool Size=5"
            })
            .Build();

        var diagnostics = PersistenceRuntimeDiagnostics.FromConfiguration(
            configuration,
            dbContextPoolSize: 64);

        Assert.Equal("Postgres", diagnostics.DatabaseProvider);
        Assert.Equal(64, diagnostics.DbContextPoolSize);
        Assert.Equal(5, diagnostics.NpgsqlMinimumPoolSize);
        Assert.Equal(37, diagnostics.NpgsqlMaximumPoolSize);
        Assert.Equal("connection-string", diagnostics.NpgsqlPoolLimitSource);
        Assert.DoesNotContain("secret", diagnostics.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromConfiguration_UsesNpgsqlDefaultsWhenPoolSizesAreNotConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=db;Port=5432;Database=portfolio;Username=portfolio;Password=secret"
            })
            .Build();

        var diagnostics = PersistenceRuntimeDiagnostics.FromConfiguration(
            configuration,
            dbContextPoolSize: 128);

        Assert.Equal(0, diagnostics.NpgsqlMinimumPoolSize);
        Assert.Equal(100, diagnostics.NpgsqlMaximumPoolSize);
        Assert.Equal("npgsql-default", diagnostics.NpgsqlPoolLimitSource);
    }
}
