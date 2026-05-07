namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Architecture)]
public sealed class NginxRuntimeConfigTests
{
    private static readonly string[] ApiProxyTimeoutDirectives =
    [
        "proxy_connect_timeout 300s;",
        "proxy_send_timeout 300s;",
        "proxy_read_timeout 300s;"
    ];

    public static TheoryData<string> RuntimeProxyConfigs => new()
    {
        "nginx/default.conf",
        "nginx/local-https.conf",
        "nginx/prod-bootstrap.conf",
        "nginx/prod.conf"
    };

    [Theory]
    [MemberData(nameof(RuntimeProxyConfigs))]
    public void ApiProxyLocations_AllowLongRunningHlsJobs(string configRelativePath)
    {
        var repositoryRoot = FindRepositoryRoot();
        var configPath = Path.Combine(repositoryRoot, configRelativePath);
        var config = File.ReadAllText(configPath);
        var apiLocationBlocks = ExtractLocationBlocks(config, "location /api/");

        Assert.NotEmpty(apiLocationBlocks);
        foreach (var block in apiLocationBlocks)
        {
            foreach (var directive in ApiProxyTimeoutDirectives)
            {
                Assert.Contains(directive, block, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void MainRuntimeAllowlist_IncludesEveryNginxConfigUnderArchitectureCoverage()
    {
        var repositoryRoot = FindRepositoryRoot();
        var allowlistPath = Path.Combine(repositoryRoot, "scripts/main-runtime-allowlist.txt");
        var allowlistEntries = File.ReadAllLines(allowlistPath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var configRelativePath in RuntimeProxyConfigs)
        {
            Assert.Contains(configRelativePath, allowlistEntries);
        }
    }

    private static IReadOnlyList<string> ExtractLocationBlocks(string config, string locationDeclaration)
    {
        var blocks = new List<string>();
        var searchIndex = 0;
        while (searchIndex < config.Length)
        {
            var locationStart = config.IndexOf(locationDeclaration, searchIndex, StringComparison.Ordinal);
            if (locationStart < 0)
            {
                break;
            }

            var blockStart = config.IndexOf('{', locationStart);
            if (blockStart < 0)
            {
                break;
            }

            var blockEnd = FindMatchingBrace(config, blockStart);
            if (blockEnd < 0)
            {
                break;
            }

            blocks.Add(config[locationStart..(blockEnd + 1)]);
            searchIndex = blockEnd + 1;
        }

        return blocks;
    }

    private static int FindMatchingBrace(string value, int blockStart)
    {
        var depth = 0;
        for (var index = blockStart; index < value.Length; index++)
        {
            depth += value[index] switch
            {
                '{' => 1,
                '}' => -1,
                _ => 0
            };

            if (depth == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "backend", "src", "WoongBlog.Api")) &&
                File.Exists(Path.Combine(directory.FullName, "package.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
