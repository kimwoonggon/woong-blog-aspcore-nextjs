using System.Data.Common;
using Microsoft.Extensions.Configuration;

namespace WoongBlog.Infrastructure.Persistence.Diagnostics;

public sealed record PersistenceRuntimeDiagnostics(
    string DatabaseProvider,
    int DbContextPoolSize,
    int? NpgsqlMinimumPoolSize,
    int? NpgsqlMaximumPoolSize,
    string NpgsqlPoolLimitSource)
{
    private const int NpgsqlDefaultMinimumPoolSize = 0;
    private const int NpgsqlDefaultMaximumPoolSize = 100;

    public static PersistenceRuntimeDiagnostics FromConfiguration(
        IConfiguration configuration,
        int dbContextPoolSize)
    {
        var databaseProvider = string.IsNullOrWhiteSpace(configuration["DatabaseProvider"])
            ? "Postgres"
            : configuration["DatabaseProvider"]!.Trim();

        if (string.Equals(databaseProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return new PersistenceRuntimeDiagnostics(
                databaseProvider,
                dbContextPoolSize,
                null,
                null,
                "not-postgres");
        }

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=portfolio;Username=portfolio;Password=portfolio";
        var poolSettings = ParseNpgsqlPoolSettings(connectionString);

        return new PersistenceRuntimeDiagnostics(
            databaseProvider,
            dbContextPoolSize,
            poolSettings.MinimumPoolSize,
            poolSettings.MaximumPoolSize,
            poolSettings.Source);
    }

    private static (int MinimumPoolSize, int MaximumPoolSize, string Source) ParseNpgsqlPoolSettings(string connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };
        var minimumPoolSize = TryGetInt32(builder, "MinimumPoolSize", "Minimum Pool Size", "MinPoolSize", "Min Pool Size");
        var maximumPoolSize = TryGetInt32(builder, "MaximumPoolSize", "Maximum Pool Size", "MaxPoolSize", "Max Pool Size");
        var source = minimumPoolSize.HasValue || maximumPoolSize.HasValue
            ? "connection-string"
            : "npgsql-default";

        return (
            minimumPoolSize ?? NpgsqlDefaultMinimumPoolSize,
            maximumPoolSize ?? NpgsqlDefaultMaximumPoolSize,
            source);
    }

    private static int? TryGetInt32(DbConnectionStringBuilder builder, params string[] names)
    {
        foreach (var name in names)
        {
            if (builder.TryGetValue(name, out var raw) && TryConvertToInt32(raw, out var value))
            {
                return value;
            }
        }

        foreach (string key in builder.Keys)
        {
            if (!names.Any(name => string.Equals(NormalizeKey(key), NormalizeKey(name), StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (builder.TryGetValue(key, out var raw) && TryConvertToInt32(raw, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool TryConvertToInt32(object? raw, out int value)
    {
        switch (raw)
        {
            case int intValue:
                value = intValue;
                return true;
            case string stringValue when int.TryParse(stringValue, out var parsed):
                value = parsed;
                return true;
            default:
                value = 0;
                return false;
        }
    }

    private static string NormalizeKey(string key)
    {
        return key.Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}
