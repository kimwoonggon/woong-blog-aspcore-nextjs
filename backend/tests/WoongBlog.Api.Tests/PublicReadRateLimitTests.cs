using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WoongBlog.Api.Tests;

public class PublicReadRateLimitTests
{
    [Fact]
    public async Task PublicReadEndpoint_ReturnsTooManyRequests_WhenLimitIsExceeded()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Security:PublicReadRateLimitPermitLimit"] = "2",
            ["Security:PublicReadRateLimitWindowSeconds"] = "60"
        });
        var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            using var request = CreatePublicRequest("203.0.113.10");
            var response = await client.SendAsync(request);
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, statuses[0]);
        Assert.Equal(HttpStatusCode.OK, statuses[1]);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[2]);
    }

    [Fact]
    public async Task PublicReadEndpoint_UsesCloudflareConnectingIpAsPartitionKey()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Security:PublicReadRateLimitPermitLimit"] = "1",
            ["Security:PublicReadRateLimitWindowSeconds"] = "60"
        });
        var client = factory.CreateClient();

        using var firstRequest = CreatePublicRequest("203.0.113.20");
        using var secondRequest = CreatePublicRequest("203.0.113.20");
        using var otherIpRequest = CreatePublicRequest("203.0.113.21");

        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);
        var otherIpResponse = await client.SendAsync(otherIpRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherIpResponse.StatusCode);
    }

    [Fact]
    public async Task PublicReadEndpoint_FallsBackToForwardedForPartitionKey()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Security:PublicReadRateLimitPermitLimit"] = "1",
            ["Security:PublicReadRateLimitWindowSeconds"] = "60"
        });
        var client = factory.CreateClient();

        using var firstRequest = CreatePublicRequest(forwardedFor: "198.51.100.10, 10.0.0.1");
        using var secondRequest = CreatePublicRequest(forwardedFor: "198.51.100.10, 10.0.0.2");
        using var otherIpRequest = CreatePublicRequest(forwardedFor: "198.51.100.11, 10.0.0.1");

        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);
        var otherIpResponse = await client.SendAsync(otherIpRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherIpResponse.StatusCode);
    }

    [Fact]
    public async Task PublicReadEndpoint_FallsBackToConnectionPartitionKey()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Security:PublicReadRateLimitPermitLimit"] = "1",
            ["Security:PublicReadRateLimitWindowSeconds"] = "60"
        });
        var client = factory.CreateClient();

        var firstResponse = await client.GetAsync("/api/public/site-settings");
        var secondResponse = await client.GetAsync("/api/public/site-settings");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    [Fact]
    public async Task PublicReadEndpoint_IsNotLimited_WhenDisabledByConfiguration()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Security:PublicReadRateLimitEnabled"] = "false",
            ["Security:PublicReadRateLimitPermitLimit"] = "1",
            ["Security:PublicReadRateLimitWindowSeconds"] = "60"
        });
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            using var request = CreatePublicRequest("203.0.113.30");
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task AdminEndpoint_DoesNotUsePublicReadPolicy()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Security:PublicReadRateLimitPermitLimit"] = "1",
            ["Security:PublicReadRateLimitWindowSeconds"] = "60"
        });
        var client = CreateAuthenticatedClient(factory);

        for (var i = 0; i < 3; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/dashboard");
            request.Headers.Add("CF-Connecting-IP", "203.0.113.40");

            var response = await client.SendAsync(request);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    [Fact]
    public void PublicGetEndpoints_UsePublicReadPolicy()
    {
        using var factory = new CustomWebApplicationFactory();
        _ = factory.CreateClient();

        var publicGetEndpoints = factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint =>
            {
                var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
                return methods?.Contains(HttpMethods.Get, StringComparer.OrdinalIgnoreCase) == true
                       && endpoint.RoutePattern.RawText?.StartsWith("/api/public/", StringComparison.OrdinalIgnoreCase) == true;
            })
            .ToList();

        Assert.NotEmpty(publicGetEndpoints);
        Assert.All(publicGetEndpoints, endpoint =>
        {
            var rateLimit = endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>();
            Assert.Equal("public-read", rateLimit?.PolicyName);
        });
    }

    private static WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> overrides)
    {
        return new PublicReadRateLimitFactory(overrides);
    }

    private static HttpRequestMessage CreatePublicRequest(string? cloudflareConnectingIp = null, string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/public/site-settings");
        if (!string.IsNullOrWhiteSpace(cloudflareConnectingIp))
        {
            request.Headers.Add("CF-Connecting-IP", cloudflareConnectingIp);
        }

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        return request;
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "admin");
        return client;
    }

    private sealed class PublicReadRateLimitFactory(Dictionary<string, string?> overrides) : CustomWebApplicationFactory
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
