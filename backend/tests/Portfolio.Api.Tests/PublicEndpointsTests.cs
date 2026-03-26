using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Api.Infrastructure.Persistence;

namespace Portfolio.Api.Tests;

public class PublicEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PublicEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetApiHealth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicSiteSettings_ReturnsSeededOwnerName()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/site-settings");

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Woonggon Kim", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicHome_ReturnsFeaturedSeedData()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/home");

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Portfolio Platform Rebuild", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Designing a Seed-First Migration Strategy", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetWorkBySlug_ReturnsSeededDetail()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/works/seeded-work");

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Portfolio Platform Rebuild", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("platform", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetWorkBySlug_ReturnsNotFound_WhenMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/works/missing-work");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicWorks_ReturnsPagedPayloadShape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/works?page=1&pageSize=1");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"items\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"page\":1", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"pageSize\":1", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPublicBlogs_ReturnsPagedPayloadShape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/blogs?page=1&pageSize=1");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"items\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"page\":1", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"pageSize\":1", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsNotFound_WhenMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/blogs/missing-blog");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicResume_ReturnsSeededResumeUrl()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/resume");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("/media/resume/woonggon-kim-resume.pdf", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SeededAdminProfile_Exists()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

        var adminProfile = await dbContext.Profiles.FindAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.NotNull(adminProfile);
        Assert.Equal("admin", adminProfile!.Role);
    }
}
