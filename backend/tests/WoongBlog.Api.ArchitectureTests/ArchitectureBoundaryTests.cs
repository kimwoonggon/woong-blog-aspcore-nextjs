using System.Reflection;
using MediatR;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Architecture)]
public class ArchitectureBoundaryTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Http_Adapters_DoNotDirectlyDependOn_DbContext()
    {
        var violatingTypes = ApiAssembly.GetTypes()
            .Where(type => type.IsClass)
            .Where(type =>
                type.Namespace == "WoongBlog.Api.Controllers" ||
                type.FullName == "WoongBlog.Api.Endpoints.AdminAiEndpoints")
            .Where(DependsOnDbContext)
            .Select(type => type.FullName)
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(violatingTypes);
    }

    [Fact]
    public void Module_Registration_Extensions_Exist_For_Approved_Modules()
    {
        var expectedTypes = new[]
        {
            "WoongBlog.Api.Common.CommonModuleServiceCollectionExtensions",
            "WoongBlog.Api.Modules.Content.Pages.PagesModuleServiceCollectionExtensions",
            "WoongBlog.Api.Modules.Content.Blogs.BlogsModuleServiceCollectionExtensions",
            "WoongBlog.Api.Modules.Content.Works.WorksModuleServiceCollectionExtensions",
            "WoongBlog.Api.Modules.Site.SiteModuleServiceCollectionExtensions",
            "WoongBlog.Api.Modules.Composition.CompositionModuleServiceCollectionExtensions",
            "WoongBlog.Api.Modules.Identity.IdentityModuleServiceCollectionExtensions",
            "WoongBlog.Api.Modules.Media.MediaModuleServiceCollectionExtensions",
            "WoongBlog.Api.Modules.AI.AiModuleServiceCollectionExtensions",
        };

        var missingTypes = expectedTypes
            .Where(typeName => ApiAssembly.GetType(typeName) is null)
            .ToArray();

        Assert.Empty(missingTypes);
    }

    [Fact]
    public void Legacy_Actor_Zones_Are_Removed()
    {
        var violatingTypes = ApiAssembly.GetTypes()
            .Where(type => type.Namespace is not null)
            .Where(type =>
            {
                var typeNamespace = type.Namespace ?? string.Empty;
                return typeNamespace.StartsWith("WoongBlog.Api.Application.Admin", StringComparison.Ordinal) ||
                    typeNamespace.StartsWith("WoongBlog.Api.Application.Public", StringComparison.Ordinal);
            })
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(violatingTypes);
    }

    [Fact]
    public void Centralized_Page_Controller_And_Request_Model_Are_Removed()
    {
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.AdminPagesController"));
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.Models.UpdatePageRequest"));
    }

    [Fact]
    public void Centralized_Blog_Controller_And_Request_Model_Are_Removed()
    {
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.AdminBlogsController"));
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.Models.SaveBlogRequest"));
    }

    [Fact]
    public void Centralized_Work_Controller_And_Request_Model_Are_Removed()
    {
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.AdminWorksController"));
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.Models.SaveWorkRequest"));
    }

    [Fact]
    public void Centralized_Public_Site_And_Dashboard_Controllers_And_Request_Model_Are_Removed()
    {
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.PublicController"));
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.AdminSiteSettingsController"));
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.AdminDashboardController"));
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.Models.UpdateSiteSettingsRequest"));
    }

    [Fact]
    public void Centralized_Identity_And_Media_Controllers_Are_Removed()
    {
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.AuthController"));
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.AdminMembersController"));
        Assert.Null(ApiAssembly.GetType("WoongBlog.Api.Controllers.UploadsController"));
    }

    [Fact]
    public void Module_Persistence_Types_DoNot_Depend_On_Other_Module_Persistence_Types()
    {
        var persistenceTypes = ApiAssembly.GetTypes()
            .Where(type => type.Namespace is not null)
            .Where(type =>
            {
                var typeNamespace = type.Namespace ?? string.Empty;
                return typeNamespace.Contains(".Modules.", StringComparison.Ordinal)
                    && typeNamespace.EndsWith(".Persistence", StringComparison.Ordinal)
                    && !typeNamespace.StartsWith("WoongBlog.Api.Modules.Composition.Persistence", StringComparison.Ordinal);
            })
            .ToArray();

        var violatingTypes = persistenceTypes
            .Where(type => DependsOnOtherModulePersistence(type, persistenceTypes))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(violatingTypes);
    }

    [Fact]
    public void Content_Handlers_DoNot_Depend_On_Actor_Facade_Services()
    {
        var disallowedFacadeNames = new HashSet<string>
        {
            "IAdminBlogService",
            "IPublicBlogService",
            "IAdminWorkService",
            "IPublicWorkService",
            "IAdminPageService",
            "IPublicPageService",
            "IAdminSiteSettingsService",
            "IPublicSiteService",
            "IAdminDashboardService",
            "IPublicHomeService",
            "IAdminMemberService",
            "IAiAdminService",
        };

        var violatingTypes = ApiAssembly.GetTypes()
            .Where(type => type.IsClass)
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("WoongBlog.Api.Modules.", StringComparison.Ordinal))
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Where(type => DependsOnTypeNamed(type, disallowedFacadeNames))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(violatingTypes);
    }

    [Fact]
    public void Cross_Module_Actor_Facade_Service_Types_Are_Removed()
    {
        var removedTypes = new[]
        {
            "WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions.IAdminBlogService",
            "WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions.IPublicBlogService",
            "WoongBlog.Api.Modules.Content.Works.Application.Abstractions.IAdminWorkService",
            "WoongBlog.Api.Modules.Content.Works.Application.Abstractions.IPublicWorkService",
            "WoongBlog.Api.Modules.Content.Pages.Application.Abstractions.IAdminPageService",
            "WoongBlog.Api.Modules.Content.Pages.Application.Abstractions.IPublicPageService",
            "WoongBlog.Api.Modules.Site.Application.Abstractions.IAdminSiteSettingsService",
            "WoongBlog.Api.Modules.Site.Application.Abstractions.IPublicSiteService",
            "WoongBlog.Api.Modules.Composition.Application.Abstractions.IAdminDashboardService",
            "WoongBlog.Api.Modules.Composition.Application.Abstractions.IPublicHomeService",
            "WoongBlog.Api.Modules.Identity.Application.Abstractions.IAdminMemberService",
            "WoongBlog.Api.Modules.AI.Application.IAiAdminService",
        };

        var stillPresent = removedTypes
            .Where(typeName => ApiAssembly.GetType(typeName) is not null)
            .ToArray();

        Assert.Empty(stillPresent);
    }

    [Fact]
    public void Module_Application_Types_DoNot_Expose_AspNetCore_Http_Results()
    {
        var violatingMembers = ApiAssembly.GetTypes()
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("WoongBlog.Api.Modules.", StringComparison.Ordinal))
            .Where(type => (type.Namespace ?? string.Empty).Contains(".Application", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Select(method => new { Type = type, Method = method }))
            .Where(candidate =>
                IsAspNetCoreHttpResult(candidate.Method.ReturnType) ||
                candidate.Method.GetParameters().Any(parameter => IsAspNetCoreHttpResult(parameter.ParameterType)))
            .Select(candidate => $"{candidate.Type.FullName}.{candidate.Method.Name}")
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(violatingMembers);
    }

    [Fact]
    public void WorkVideo_Application_Result_DoesNot_Use_AspNetCore_StatusCodes()
    {
        var workVideoApplicationDirectory = Path.Combine(
            FindRepositoryRoot(),
            "backend",
            "src",
            "WoongBlog.Api",
            "Modules",
            "Content",
            "Works",
            "Application",
            "WorkVideos");

        var violatingFiles = Directory.EnumerateFiles(workVideoApplicationDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("StatusCodes.", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path))
            .OrderBy(path => path)
            .ToArray();

        Assert.Empty(violatingFiles);
    }

    [Fact]
    public void Ai_Application_Types_DoNot_Directly_Use_ServiceScopeFactory_Or_ServiceLocator()
    {
        var aiApplicationDirectory = Path.Combine(
            FindRepositoryRoot(),
            "backend",
            "src",
            "WoongBlog.Api",
            "Modules",
            "AI",
            "Application");

        var disallowedTokens = new[]
        {
            "IServiceScopeFactory",
            ".CreateScope(",
            ".CreateAsyncScope(",
            "GetRequiredService<",
        };

        var violatingFiles = Directory.EnumerateFiles(aiApplicationDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return disallowedTokens.Any(token => source.Contains(token, StringComparison.Ordinal));
            })
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path))
            .OrderBy(path => path)
            .ToArray();

        Assert.Empty(violatingFiles);
    }

    [Fact]
    public void Content_Application_Abstractions_DoNot_Accept_MediatR_Request_Types()
    {
        var abstractionTypes = ApiAssembly.GetTypes()
            .Where(type => type.IsInterface)
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("WoongBlog.Api.Modules.", StringComparison.Ordinal))
            .Where(type => (type.Namespace ?? string.Empty).Contains(".Application.Abstractions", StringComparison.Ordinal))
            .ToArray();

        var violatingMethods = abstractionTypes
            .SelectMany(type => type.GetMethods().Select(method => new { Type = type, Method = method }))
            .Where(candidate => candidate.Method.GetParameters().Any(parameter => IsMediatRRequestType(parameter.ParameterType)))
            .Select(candidate => $"{candidate.Type.FullName}.{candidate.Method.Name}")
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(violatingMethods);
    }

    private static bool DependsOnDbContext(Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(field => field.FieldType == typeof(WoongBlogDbContext));
    }

    private static bool DependsOnOtherModulePersistence(Type type, IReadOnlyCollection<Type> persistenceTypes)
    {
        var ownNamespace = type.Namespace ?? string.Empty;
        var otherPersistenceTypes = persistenceTypes
            .Where(candidate => candidate != type)
            .Where(candidate => candidate.Namespace != ownNamespace)
            .ToHashSet();

        return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(field => field.FieldType)
                .Concat(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .SelectMany(constructor => constructor.GetParameters())
                    .Select(parameter => parameter.ParameterType))
                .Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .SelectMany(method => method.GetParameters())
                    .Select(parameter => parameter.ParameterType))
                .Any(otherPersistenceTypes.Contains);
    }

    private static bool DependsOnTypeNamed(Type type, IReadOnlySet<string> typeNames)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(field => field.FieldType)
            .Any(fieldType => IsTypeNamed(fieldType, typeNames));
    }

    private static bool IsTypeFromNamespaces(Type type, IReadOnlyCollection<string> namespaces)
    {
        if (type.IsGenericType)
        {
            return type.GetGenericArguments().Any(argument => IsTypeFromNamespaces(argument, namespaces));
        }

        var namespaceName = type.Namespace;
        return namespaceName is not null && namespaces.Any(namespaceName.StartsWith);
    }

    private static bool IsTypeNamed(Type type, IReadOnlySet<string> typeNames)
    {
        if (type.IsGenericType)
        {
            return type.GetGenericArguments().Any(argument => IsTypeNamed(argument, typeNames));
        }

        return typeNames.Contains(type.Name);
    }

    private static bool IsAspNetCoreHttpResult(Type type)
    {
        return type.FullName == "Microsoft.AspNetCore.Http.IResult" ||
            type.GetInterfaces().Any(candidate => candidate.FullName == "Microsoft.AspNetCore.Http.IResult");
    }

    private static bool IsMediatRRequestType(Type type)
    {
        return typeof(IRequest).IsAssignableFrom(type) ||
            type.GetInterfaces().Any(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "backend", "src", "WoongBlog.Api")) &&
                File.Exists(Path.Combine(directory.FullName, "package.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
