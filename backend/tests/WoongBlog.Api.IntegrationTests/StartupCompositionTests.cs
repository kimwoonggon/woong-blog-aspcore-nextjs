using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WoongBlog.Api.Tests;

public class StartupCompositionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StartupCompositionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Root_RedirectsToHealthEndpoint()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/api/health", response.Headers.Location?.OriginalString);
    }
}
