using PactNet.Verifier;
using PactNet;
using PactNet.Output.Xunit;
using Xunit.Abstractions;

namespace WoongBlog.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class ProviderContractVerificationTests
{
    private readonly ITestOutputHelper _output;

    public ProviderContractVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [PactProviderFact]
    public void VerifyProviderContractsFromPactFiles()
    {
        var providerBaseUrl = Environment.GetEnvironmentVariable("PACT_PROVIDER_BASE_URL") ??
            throw new InvalidOperationException(
                PactProviderContractSetup.SkipReasonIfUnavailable() ?? "PACT_PROVIDER_BASE_URL is required.");
        var pactFiles = PactProviderContractSetup.GetPactFiles();

        foreach (var pactFile in pactFiles)
        {
            using var verifier = new PactVerifier("WoongBlog API", new PactVerifierConfig
            {
                Outputters = [new XunitOutput(_output)],
                LogLevel = PactLogLevel.Debug
            });

            verifier
                .WithHttpEndpoint(new Uri(providerBaseUrl))
                .WithFileSource(new FileInfo(pactFile))
                .WithRequestTimeout(TimeSpan.FromSeconds(30))
                .Verify();
        }
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class PactProviderFactAttribute : FactAttribute
{
    public PactProviderFactAttribute()
    {
        Skip = PactProviderContractSetup.SkipReasonIfUnavailable();
    }
}

internal static class PactProviderContractSetup
{
    public static string[] GetPactFiles()
    {
        var pactDirectory = ResolvePactDirectory();
        return Directory.Exists(pactDirectory)
            ? Directory.EnumerateFiles(pactDirectory, "*.json", SearchOption.TopDirectoryOnly).ToArray()
            : [];
    }

    public static string? SkipReasonIfUnavailable()
    {
        var pactDirectory = ResolvePactDirectory();
        var providerBaseUrl = Environment.GetEnvironmentVariable("PACT_PROVIDER_BASE_URL");
        var pactFilesExist = Directory.Exists(pactDirectory) &&
            Directory.EnumerateFiles(pactDirectory, "*.json", SearchOption.TopDirectoryOnly).Any();

        return string.IsNullOrWhiteSpace(providerBaseUrl) || !pactFilesExist
            ? "Pact provider verification requires PACT_PROVIDER_BASE_URL and at least one pact file in " +
              $"{pactDirectory}. Start the ASP.NET provider on a real local TCP socket and set PACT_PROVIDER_BASE_URL to enable verification."
            : null;
    }

    private static string ResolvePactDirectory()
    {
        var configuredDirectory = Environment.GetEnvironmentVariable("PACT_FILE_DIRECTORY");
        return !string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.GetFullPath(configuredDirectory)
            : Path.Combine(FindRepositoryRoot(), "tests", "contracts", "pacts");
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
