using System.Net;
using System.Net.Http.Json;

namespace Portfolio.Api.Tests;

public class AdminDashboardEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminDashboardEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDashboardSummary_ReturnsCountsForAdmin()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/admin/dashboard");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<DashboardSummary>();

        Assert.NotNull(payload);
        Assert.True(payload is { WorksCount: >= 1, BlogsCount: >= 1, ViewsCount: >= 0 });
    }

    [Fact]
    public async Task GetDashboardSummary_RejectsAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class DashboardSummary
    {
        public int WorksCount { get; set; }
        public int BlogsCount { get; set; }
        public int ViewsCount { get; set; }
    }
}
