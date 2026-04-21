using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Modules.Identity.Application;

namespace WoongBlog.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _mediaRoot = Path.Combine(Path.GetTempPath(), $"portfolio-tests-media-{Guid.NewGuid():N}");
    private readonly string _dataProtectionRoot = Path.Combine(Path.GetTempPath(), $"portfolio-tests-dp-{Guid.NewGuid():N}");

    public new HttpClient CreateClient(WebApplicationFactoryClientOptions? options = null)
    {
        var resolvedOptions = options ?? new WebApplicationFactoryClientOptions();
        resolvedOptions.BaseAddress ??= new Uri("https://localhost");
        return base.CreateClient(resolvedOptions);
    }

    public HttpClient CreateAuthenticatedClient(WebApplicationFactoryClientOptions? options = null, bool includeCsrf = true)
    {
        var resolvedOptions = options ?? new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        };
        resolvedOptions.BaseAddress ??= new Uri("https://localhost");

        var client = CreateClient(resolvedOptions);
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "admin");

        if (includeCsrf)
        {
            var csrfPayload = client.GetFromJsonAsync<CsrfTokenResponse>("/api/auth/csrf").GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(csrfPayload?.RequestToken))
            {
                client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
                client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfPayload.RequestToken);
            }
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_mediaRoot);
        Directory.CreateDirectory(_dataProtectionRoot);

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "InMemory",
                ["InMemoryDatabaseName"] = $"portfolio-tests-{Guid.NewGuid()}",
                ["Auth:Enabled"] = "true",
                ["Auth:Authority"] = "https://example.test",
                ["Auth:ClientId"] = "test-client",
                ["Auth:ClientSecret"] = "test-secret",
                ["Auth:MediaRoot"] = _mediaRoot,
                ["Auth:DataProtectionKeysPath"] = _dataProtectionRoot,
                ["Auth:SecureCookies"] = "false",
                ["Security:UseHttpsRedirection"] = "false",
                ["Security:UseHsts"] = "false",
                ["Proxy:KnownProxies:0"] = "127.0.0.1"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireClaim(AuthClaimTypes.Role, "admin"));
            });
        });
    }

    void IDisposable.Dispose()
    {
        base.Dispose();

        if (Directory.Exists(_mediaRoot))
        {
            Directory.Delete(_mediaRoot, recursive: true);
        }

        if (Directory.Exists(_dataProtectionRoot))
        {
            Directory.Delete(_dataProtectionRoot, recursive: true);
        }
    }

    private sealed record CsrfTokenResponse(string RequestToken, string HeaderName);
}
