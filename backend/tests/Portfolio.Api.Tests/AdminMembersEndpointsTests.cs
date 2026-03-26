using System.Net;

namespace Portfolio.Api.Tests;

public class AdminMembersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminMembersEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMembers_ReturnsPrivacySafeMemberListForAdmin()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/admin/members");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("admin@example.com", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"activeSessionCount\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("providerSubject", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ipAddress", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMembers_RejectsAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/members");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
