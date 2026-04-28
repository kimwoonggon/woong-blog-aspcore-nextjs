using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Integration)]
public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSession_WhenAnonymous_ReturnsUnauthenticated()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/session");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"authenticated\":false", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSession_WhenAuthenticated_ReturnsAdminSession()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/auth/session");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"authenticated\":true", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"role\":\"admin\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCsrf_ReturnsRequestToken()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/auth/csrf");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("requestToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("X-CSRF-TOKEN", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestLogin_IsNotRateLimitedByTheApplication()
    {
        using var isolatedFactory = new CustomWebApplicationFactory();
        var client = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/api/auth/test-login?email=admin@example.com&returnUrl=%2Fadmin");
            statuses.Add(response.StatusCode);
        }

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statuses);
    }

    [Fact]
    public async Task Session_IsNotRateLimited_WhenFlooded()
    {
        using var isolatedFactory = new CustomWebApplicationFactory();
        var client = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        for (var i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/api/auth/session");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task LogoutGet_ReturnsMethodNotAllowed()
    {
        var client = _factory.CreateAuthenticatedClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/auth/logout");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
