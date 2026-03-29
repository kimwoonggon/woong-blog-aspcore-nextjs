using System.Reflection;
using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
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
    public void PublicBlogService_ListSignature_UsesPrimitivePaging()
    {
        var method = typeof(IPublicBlogService).GetMethod(nameof(IPublicBlogService.GetBlogsAsync));

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Collection(parameters,
            parameter => Assert.Equal(typeof(int), parameter.ParameterType),
            parameter => Assert.Equal(typeof(int), parameter.ParameterType),
            parameter => Assert.Equal(typeof(CancellationToken), parameter.ParameterType));
    }

    [Fact]
    public void PublicWorkService_ListSignature_UsesPrimitivePaging()
    {
        var method = typeof(IPublicWorkService).GetMethod(nameof(IPublicWorkService.GetWorksAsync));

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Collection(parameters,
            parameter => Assert.Equal(typeof(int), parameter.ParameterType),
            parameter => Assert.Equal(typeof(int), parameter.ParameterType),
            parameter => Assert.Equal(typeof(CancellationToken), parameter.ParameterType));
    }

    private static bool ImplementsMediatRRequest(Type parameterType)
    {
        return parameterType.GetInterfaces().Any(interfaceType =>
            interfaceType == typeof(IRequest)
            || (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRequest<>)));
    }
}
