using System.Text.Json;
using System.Xml.Linq;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Architecture)]
public class BackendCoverageToolingTests
{
    [Fact]
    public void BackendCoverageScript_ExposesExpectedSuitesAndOutputLayout()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "run-backend-coverage.sh");

        Assert.True(File.Exists(scriptPath), $"Expected backend coverage script at {scriptPath}.");

        var script = File.ReadAllText(scriptPath);
        Assert.Contains("coverage/backend", script);
        Assert.Contains("WoongBlog.Api.UnitTests.csproj", script);
        Assert.Contains("WoongBlog.Api.ComponentTests.csproj", script);
        Assert.Contains("WoongBlog.Api.IntegrationTests.csproj", script);
        Assert.Contains("backend/WoongBlog.sln", script);
        Assert.Contains("XPlat Code Coverage", script);
        Assert.Contains("reportgenerator", script);
    }

    [Fact]
    public void BackendCoverageRunsettings_InstrumentsProductionAssembliesOnly()
    {
        var repositoryRoot = FindRepositoryRoot();
        var runsettingsPath = Path.Combine(repositoryRoot, "backend", "coverage.runsettings");

        Assert.True(File.Exists(runsettingsPath), $"Expected backend coverage runsettings at {runsettingsPath}.");

        var settings = XDocument.Load(runsettingsPath);
        var configuration = settings.Descendants("Configuration").Single();
        var format = configuration.Element("Format")?.Value ?? string.Empty;
        var include = configuration.Element("Include")?.Value ?? string.Empty;
        var exclude = configuration.Element("Exclude")?.Value ?? string.Empty;
        var excludeByFile = configuration.Element("ExcludeByFile")?.Value ?? string.Empty;

        Assert.Contains("cobertura", format, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[WoongBlog.Api]*", include);
        Assert.Contains("[WoongBlog.Application]*", include);
        Assert.Contains("[WoongBlog.Domain]*", include);
        Assert.Contains("[WoongBlog.Infrastructure]*", include);
        Assert.Contains("[*.Tests]*", exclude);
        Assert.Contains("**/bin/**/*.cs", excludeByFile);
        Assert.Contains("**/obj/**/*.cs", excludeByFile);
    }

    [Fact]
    public void BackendReportGeneratorTool_IsPinnedInLocalManifest()
    {
        var repositoryRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repositoryRoot, "backend", ".config", "dotnet-tools.json");

        Assert.True(File.Exists(manifestPath), $"Expected backend tool manifest at {manifestPath}.");

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var tool = manifest.RootElement
            .GetProperty("tools")
            .GetProperty("dotnet-reportgenerator-globaltool");

        Assert.False(string.IsNullOrWhiteSpace(tool.GetProperty("version").GetString()));
        Assert.Contains(
            tool.GetProperty("commands").EnumerateArray(),
            command => command.GetString() == "reportgenerator");
    }

    [Fact]
    public void BackendTestingDocumentation_ExplainsCoverageUseAndLimits()
    {
        var repositoryRoot = FindRepositoryRoot();
        var testingPath = Path.Combine(repositoryRoot, "backend", "TESTING.md");

        var documentation = File.ReadAllText(testingPath);
        Assert.Contains("./scripts/run-backend-coverage.sh unit", documentation);
        Assert.Contains("./scripts/run-backend-coverage.sh component", documentation);
        Assert.Contains("./scripts/run-backend-coverage.sh integration", documentation);
        Assert.Contains("./scripts/run-backend-coverage.sh full", documentation);
        Assert.Contains("coverage/backend", documentation);
        Assert.Contains("coverage percentage", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Component", documentation);
        Assert.Contains("Integration", documentation);
        Assert.Contains("Unit", documentation);
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
