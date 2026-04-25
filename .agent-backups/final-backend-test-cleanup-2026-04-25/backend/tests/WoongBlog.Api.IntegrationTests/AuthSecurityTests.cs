using System.Net;
using System.Net.Http.Json;

namespace WoongBlog.Api.Tests;

public class AuthSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthSecurityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateSiteSettings_WithoutCsrf_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(includeCsrf: false);

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName = "CSRF Blocked"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSiteSettings_WithCsrf_ReturnsOk()
    {
        var client = _factory.CreateAuthenticatedClient();
        var csrfResponse = await client.GetAsync("/api/auth/csrf");
        csrfResponse.EnsureSuccessStatusCode();
        var csrfPayload = await csrfResponse.Content.ReadFromJsonAsync<CsrfResponse>();
        Assert.NotNull(csrfPayload?.RequestToken);

        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add(csrfPayload!.HeaderName, csrfPayload.RequestToken);

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName = "CSRF Allowed"
        });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Session_Response_ContainsSecurityHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/session");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var nosniffHeader));
        Assert.Contains("nosniff", nosniffHeader);
        Assert.True(response.Headers.TryGetValues("Referrer-Policy", out var referrerHeader));
        Assert.Contains("strict-origin-when-cross-origin", referrerHeader);
        Assert.True(response.Headers.TryGetValues("Permissions-Policy", out var permissionsHeader));
        Assert.Contains("camera=()", string.Join(", ", permissionsHeader!));
        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var cspHeader));
        Assert.Contains("default-src 'self'", string.Join(", ", cspHeader!));
    }

    private sealed record CsrfResponse(string RequestToken, string HeaderName);
}
