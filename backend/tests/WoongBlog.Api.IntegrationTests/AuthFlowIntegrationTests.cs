using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WoongBlog.Infrastructure.Persistence;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Integration)]
public class AuthFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSession_WhenAdminHeaderPresent_ReturnsFullAdminPayload()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/auth/session");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SessionPayload>();

        Assert.NotNull(payload);
        Assert.True(payload!.Authenticated);
        Assert.Equal("Admin Example", payload.Name);
        Assert.Equal("admin@example.com", payload.Email);
        Assert.Equal("admin", payload.Role);
        Assert.Equal("11111111-1111-1111-1111-111111111111", payload.ProfileId);
    }

    [Fact]
    public async Task GetSession_WhenNonAdminHeaderPresent_ReturnsAuthenticatedUserRole()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "user");

        var response = await client.GetAsync("/api/auth/session");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SessionPayload>();

        Assert.NotNull(payload);
        Assert.True(payload!.Authenticated);
        Assert.Equal("User Example", payload.Name);
        Assert.Equal("user@example.com", payload.Email);
        Assert.Equal("user", payload.Role);
        Assert.Equal("33333333-3333-3333-3333-333333333333", payload.ProfileId);
    }

    [Theory]
    [InlineData("/api/admin/pages")]
    [InlineData("/api/admin/blogs")]
    [InlineData("/api/admin/works")]
    [InlineData("/api/admin/site-settings")]
    [InlineData("/api/admin/dashboard")]
    [InlineData("/api/admin/members")]
    [InlineData("/api/admin/ai/runtime-config")]
    public async Task AdminGetEndpoints_WhenAnonymous_ReturnUnauthorized(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("/api/admin/pages")]
    [InlineData("/api/admin/blogs")]
    [InlineData("/api/admin/works")]
    [InlineData("/api/admin/site-settings")]
    [InlineData("/api/admin/dashboard")]
    [InlineData("/api/admin/members")]
    [InlineData("/api/admin/ai/runtime-config")]
    public async Task AdminGetEndpoints_WhenAuthenticatedWithoutAdminRole_ReturnForbidden(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "user");

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/pages")]
    [InlineData("/api/admin/blogs")]
    [InlineData("/api/admin/works")]
    [InlineData("/api/admin/site-settings")]
    [InlineData("/api/admin/dashboard")]
    [InlineData("/api/admin/members")]
    [InlineData("/api/admin/ai/runtime-config")]
    public async Task AdminGetEndpoints_WhenAuthenticatedAsAdmin_ReturnSuccess(string path)
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync(path);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_WhenAuthConfigured_ChallengesFakeOidcProvider()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/auth/login?returnUrl=%2Fadmin%2Fpages");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("https://example.test/oauth/authorize", location, StringComparison.Ordinal);
        Assert.Contains("client_id=test-client", location, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=", location, StringComparison.Ordinal);
        Assert.Contains("%2Fapi%2Fauth%2Fcallback", location, StringComparison.Ordinal);
        Assert.Contains("return_url=%2Fadmin%2Fpages", location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_WhenReturnUrlIsExternal_DoesNotExposeExternalRedirectTarget()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/auth/login?returnUrl=https%3A%2F%2Fevil.test%2Fsteal");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var location = response.Headers.Location!.ToString();
        Assert.Contains("return_url=%2Fadmin", location, StringComparison.Ordinal);
        Assert.DoesNotContain("evil.test", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogoutPost_WhenCsrfMissing_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(includeCsrf: false);

        var response = await client.PostAsJsonAsync("/api/auth/logout", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LogoutPost_WhenCsrfValid_ReturnsRedirectPayloadAndClearsAuthCookie()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/auth/logout?returnUrl=%2Fgoodbye", new { });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LogoutPayload>();
        Assert.NotNull(payload);
        Assert.Equal("/goodbye", payload!.RedirectUrl);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders));
        Assert.Contains(setCookieHeaders!, header =>
            header.Contains("portfolio_auth=", StringComparison.OrdinalIgnoreCase)
            && header.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateSiteSettings_WhenCsrfTokenInvalid_ReturnsBadRequestAndDoesNotPersist()
    {
        var originalOwnerName = $"Original Owner {Guid.NewGuid():N}";
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
            var settings = dbContext.SiteSettings.Single(x => x.Singleton);
            settings.OwnerName = originalOwnerName;
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateAuthenticatedClient(includeCsrf: false);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid-token");

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName = "Should Not Persist"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        Assert.Equal(originalOwnerName, verificationDb.SiteSettings.Single(x => x.Singleton).OwnerName);
    }

    [Fact]
    public async Task UpdateSiteSettings_WhenCsrfTokenValid_PersistsMutation()
    {
        var ownerName = $"CSRF Valid Owner {Guid.NewGuid():N}";
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName
        });

        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
        Assert.Equal(ownerName, dbContext.SiteSettings.Single(x => x.Singleton).OwnerName);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("user", HttpStatusCode.Forbidden)]
    public async Task AdminMutation_WhenCsrfTokenValidButPrincipalIsNotAdmin_ReturnsAuthFailure(
        string? identity,
        HttpStatusCode expectedStatus)
    {
        var client = await CreateClientWithCsrfAsync(identity);

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new
        {
            ownerName = "Blocked By Authorization"
        });

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    private async Task<HttpClient> CreateClientWithCsrfAsync(string? identity)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (!string.IsNullOrWhiteSpace(identity))
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, identity);
        }

        var csrfPayload = await client.GetFromJsonAsync<CsrfPayload>("/api/auth/csrf");
        Assert.NotNull(csrfPayload?.RequestToken);
        client.DefaultRequestHeaders.Add(csrfPayload!.HeaderName, csrfPayload.RequestToken);
        return client;
    }

    private sealed record SessionPayload(
        bool Authenticated,
        string? Name,
        string? Email,
        string? Role,
        string? ProfileId);

    private sealed record LogoutPayload(string RedirectUrl);

    private sealed record CsrfPayload(string RequestToken, string HeaderName);
}
