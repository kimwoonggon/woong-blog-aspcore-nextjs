using PactNet.Verifier;
using PactNet;
using PactNet.Output.Xunit;
using Xunit.Abstractions;

namespace WoongBlog.Api.ContractTests;

public sealed class ProviderContractVerificationTests
{
    private readonly ITestOutputHelper _output;

    public ProviderContractVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void VerifyProviderContractsFromPactFiles()
    {
        var providerBaseUrl = Environment.GetEnvironmentVariable("PACT_PROVIDER_BASE_URL");
        var pactDirectory = ResolvePactDirectory();
        var pactFiles = Directory.Exists(pactDirectory)
            ? Directory.EnumerateFiles(pactDirectory, "*.json", SearchOption.TopDirectoryOnly).ToArray()
            : [];

        if (string.IsNullOrWhiteSpace(providerBaseUrl) || pactFiles.Length == 0)
        {
            _output.WriteLine(
                "Skipping Pact provider verification because PACT_PROVIDER_BASE_URL is unset or no pact files exist in " +
                $"{pactDirectory}. Start the ASP.NET provider on a real local TCP socket and set PACT_PROVIDER_BASE_URL to enable verification.");
            return;
        }

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

    private static string ResolvePactDirectory()
    {
        var configuredDirectory = Environment.GetEnvironmentVariable("PACT_FILE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return Path.GetFullPath(configuredDirectory);
        }

        return Path.Combine(FindRepositoryRoot(), "tests", "contracts", "pacts");
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
