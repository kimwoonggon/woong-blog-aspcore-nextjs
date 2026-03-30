using System.Reflection;
using Microsoft.AspNetCore.Http;
using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.GetAdminWorks;
using WoongBlog.Api.Controllers;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Endpoints;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Tests;

public class ApplicationBoundaryContractTests
{
    [Theory]
    [InlineData(typeof(IAdminBlogWriteStore))]
    [InlineData(typeof(IAdminWorkWriteStore))]
    [InlineData(typeof(IAdminPageWriteStore))]
    [InlineData(typeof(IAdminSiteSettingsWriteStore))]
    public void WriteStorePorts_DoNotAcceptMediatRRequests(Type portType)
    {
        var offendingParameters = portType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(method => method.GetParameters())
            .Where(parameter => ImplementsMediatRRequest(parameter.ParameterType))
            .ToList();

        Assert.Empty(offendingParameters);
    }

    [Fact]
    public void PublicBlogQueries_ListSignature_UsesPrimitivePaging()
    {
        var method = typeof(IPublicBlogQueries).GetMethod(nameof(IPublicBlogQueries.GetBlogsAsync));

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Collection(parameters,
            parameter => Assert.Equal(typeof(int), parameter.ParameterType),
            parameter => Assert.Equal(typeof(int), parameter.ParameterType),
            parameter => Assert.Equal(typeof(CancellationToken), parameter.ParameterType));
    }

    [Fact]
    public void PublicWorkQueries_ListSignature_UsesPrimitivePaging()
    {
        var method = typeof(IPublicWorkQueries).GetMethod(nameof(IPublicWorkQueries.GetWorksAsync));

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Collection(parameters,
            parameter => Assert.Equal(typeof(int), parameter.ParameterType),
            parameter => Assert.Equal(typeof(int), parameter.ParameterType),
            parameter => Assert.Equal(typeof(CancellationToken), parameter.ParameterType));
    }

    [Fact]
    public void QueryHandlers_DependOn_QueryAbstractions_Not_ServiceNamed_ReadPorts()
    {
        var handlerTypes = typeof(GetAdminWorksQueryHandler).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
            .ToList();

        var offendingParameters = handlerTypes
            .SelectMany(type => type.GetConstructors())
            .SelectMany(constructor => constructor.GetParameters())
            .Where(parameter =>
                parameter.ParameterType.IsInterface
                && parameter.ParameterType.Namespace is not null
                && parameter.ParameterType.Namespace.Contains(".Application.", StringComparison.Ordinal)
                && parameter.ParameterType.Name.EndsWith("Service", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(offendingParameters);
    }

    [Fact]
    public void AuthController_DoesNotDependOn_DbContext()
    {
        var constructorParameters = typeof(AuthController)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToList();

        Assert.DoesNotContain(constructorParameters, type => type.Name == "WoongBlogDbContext");
    }

    [Fact]
    public void AdminAiWorkflowService_DoesNot_Return_HttpResults()
    {
        var offendingMethods = typeof(IAdminAiWorkflowService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => typeof(IResult).IsAssignableFrom(method.ReturnType)
                || (method.ReturnType.IsGenericType
                    && method.ReturnType.GetGenericArguments().Any(type => typeof(IResult).IsAssignableFrom(type))))
            .ToList();

        Assert.Empty(offendingMethods);
    }

    [Theory]
    [InlineData(typeof(Work), nameof(Work.Title))]
    [InlineData(typeof(Work), nameof(Work.Slug))]
    [InlineData(typeof(Work), nameof(Work.Excerpt))]
    [InlineData(typeof(Blog), nameof(Blog.Title))]
    [InlineData(typeof(Blog), nameof(Blog.Slug))]
    [InlineData(typeof(PageEntity), nameof(PageEntity.Title))]
    [InlineData(typeof(SiteSetting), nameof(SiteSetting.OwnerName))]
    [InlineData(typeof(Profile), nameof(Profile.Email))]
    [InlineData(typeof(AuthSession), nameof(AuthSession.LastSeenAt))]
    [InlineData(typeof(Asset), nameof(Asset.Path))]
    [InlineData(typeof(AiBatchJob), nameof(AiBatchJob.Status))]
    [InlineData(typeof(AiBatchJobItem), nameof(AiBatchJobItem.Status))]
    public void DomainEntities_Use_NonPublicSetters_For_MutableState(Type entityType, string propertyName)
    {
        var property = entityType.GetProperty(propertyName);

        Assert.NotNull(property);
        Assert.NotNull(property!.SetMethod);
        Assert.False(property.SetMethod!.IsPublic);
    }

    private static bool ImplementsMediatRRequest(Type parameterType)
    {
        return parameterType.GetInterfaces().Any(interfaceType =>
            interfaceType == typeof(IRequest)
            || (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRequest<>)));
    }
}
