using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit.Sdk;

namespace WoongBlog.Api.Tests;

public class StartupOptionsValidationTests
{
    [Fact]
    public async Task CurrentTestingConfiguration_RemainsValid()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public void InvalidProxyNetwork_FailsAtStartup()
    {
        using var factory = CreateInvalidFactory(new Dictionary<string, string?>
        {
            ["Proxy:KnownNetworks:0"] = "not-a-cidr"
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var validationException = FindOptionsValidationException(exception);

        Assert.Contains("Proxy:KnownNetworks", string.Join('\n', validationException.Failures));
    }

    [Fact]
    public void InvalidAuthPublicOrigin_FailsAtStartup()
    {
        using var factory = CreateInvalidFactory(new Dictionary<string, string?>
        {
            ["Auth:PublicOrigin"] = "https://woonglab.com/login"
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var validationException = FindOptionsValidationException(exception);

        Assert.Contains("Auth:PublicOrigin", string.Join('\n', validationException.Failures));
    }

    [Fact]
    public void InvalidAiProvider_FailsAtStartup()
    {
        using var factory = CreateInvalidFactory(new Dictionary<string, string?>
        {
            ["Ai:Provider"] = "bogus-provider"
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var validationException = FindOptionsValidationException(exception);

        Assert.Contains("Ai:Provider", string.Join('\n', validationException.Failures));
    }

    [Fact]
    public void ProductionAuthWithoutCredentials_FailsAtStartup()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DatabaseProvider"] = "InMemory",
                    ["InMemoryDatabaseName"] = $"portfolio-tests-{Guid.NewGuid()}",
                    ["Auth:Enabled"] = "true",
                    ["Auth:Authority"] = "https://example.test",
                    ["Auth:ClientId"] = "",
                    ["Auth:ClientSecret"] = "",
                    ["Auth:DataProtectionKeysPath"] = Path.Combine(Path.GetTempPath(), $"portfolio-tests-dp-{Guid.NewGuid():N}"),
                    ["Auth:MediaRoot"] = Path.Combine(Path.GetTempPath(), $"portfolio-tests-media-{Guid.NewGuid():N}"),
                    ["Security:UseHttpsRedirection"] = "false",
                    ["Security:UseHsts"] = "false",
                    ["Proxy:KnownProxies:0"] = "127.0.0.1"
                });
            });
        });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var validationException = FindOptionsValidationException(exception);

        Assert.Contains("Auth is enabled outside Development/Testing", string.Join('\n', validationException.Failures));
    }

    private static CustomWebApplicationFactory CreateInvalidFactory(Dictionary<string, string?> overrides)
    {
        return new InvalidConfigurationFactory(overrides);
    }

    private static OptionsValidationException FindOptionsValidationException(Exception exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is OptionsValidationException validationException)
            {
                return validationException;
            }

            current = current.InnerException;
        }

        throw new XunitException($"Expected an {nameof(OptionsValidationException)} but got: {exception}");
    }

    private sealed class InvalidConfigurationFactory(Dictionary<string, string?> overrides) : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(overrides);
            });
        }
    }
}
