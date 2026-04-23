using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Blogs.Api.CreateBlog;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Blogs.Application.CreateBlog;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

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

    [Fact]
    public void ServiceProvider_ResolvesApiApplicationAndInfrastructureServices()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        Assert.NotNull(services.GetRequiredService<IMediator>());
        Assert.NotNull(services.GetRequiredService<IAiBatchTargetQueryStore>());
        Assert.NotNull(services.GetRequiredService<IAiBatchJobQueryStore>());
        Assert.NotNull(services.GetRequiredService<IAiBatchJobCommandStore>());
        Assert.NotNull(services.GetRequiredService<IValidator<CreateBlogRequest>>());
        Assert.NotNull(services.GetRequiredService<IValidator<CreateBlogCommand>>());
        Assert.NotNull(services.GetRequiredService<IRequestHandler<CreateBlogCommand, AdminMutationResult>>());
        Assert.NotNull(services.GetRequiredService<IBlogCommandStore>());
        Assert.NotNull(services.GetRequiredService<IWorkVideoCleanupStore>());
        Assert.NotNull(services.GetRequiredService<WoongBlogDbContext>());
    }

    [Fact]
    public void AuthOptions_Use300MinuteSlidingExpiration_AndEightHourAbsoluteExpiration()
    {
        using var scope = _factory.Services.CreateScope();
        var authOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;

        Assert.Equal(300, authOptions.SlidingExpirationMinutes);
        Assert.Equal(8, authOptions.AbsoluteExpirationHours);
    }
}
