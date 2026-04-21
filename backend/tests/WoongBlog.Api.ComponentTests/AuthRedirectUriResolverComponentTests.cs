using Microsoft.AspNetCore.Http;
using WoongBlog.Api.Infrastructure.Auth;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public class AuthRedirectUriResolverComponentTests
{
    [Fact]
    public void ResolveCallbackUri_UsesConfiguredPublicOrigin_WhenPresent()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("backend", 8080);

        var result = PublicOriginUrlResolver.ResolveCallbackUri(context.Request, new AuthOptions
        {
            PublicOrigin = "https://woonglab.com",
            CallbackPath = "/api/auth/callback"
        });

        Assert.Equal("https://woonglab.com/api/auth/callback", result);
    }

    [Fact]
    public void ResolveCallbackUri_FallsBackToRequestOrigin_WhenPublicOriginMissing()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("www.woonglab.com");

        var result = PublicOriginUrlResolver.ResolveCallbackUri(context.Request, new AuthOptions
        {
            CallbackPath = "/api/auth/callback"
        });

        Assert.Equal("https://www.woonglab.com/api/auth/callback", result);
    }
}
