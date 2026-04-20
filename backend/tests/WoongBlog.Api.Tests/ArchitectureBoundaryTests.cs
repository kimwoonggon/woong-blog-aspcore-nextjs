using System.Reflection;
using MediatR;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Tests;

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
        };

        var violatingTypes = ApiAssembly.GetTypes()
            .Where(type => type.IsClass)
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("WoongBlog.Api.Modules.Content.", StringComparison.Ordinal))
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Where(type => DependsOnTypeNamed(type, disallowedFacadeNames))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(violatingTypes);
    }

    [Fact]
    public void Content_Application_Abstractions_DoNot_Accept_MediatR_Request_Types()
    {
        var abstractionTypes = ApiAssembly.GetTypes()
            .Where(type => type.IsInterface)
            .Where(type => (type.Namespace ?? string.Empty).StartsWith("WoongBlog.Api.Modules.Content.", StringComparison.Ordinal))
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

    [Fact]
    public void Domain_Source_DoesNot_Reference_Application_Modules_Or_Infrastructure()
    {
        var domainDirectory = Path.Combine(FindRepositoryRoot(), "backend", "src", "WoongBlog.Api", "Domain");
        var violatingFiles = Directory.EnumerateFiles(domainDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("using WoongBlog.Api.Modules.", StringComparison.Ordinal) ||
                    source.Contains("using WoongBlog.Api.Application", StringComparison.Ordinal) ||
                    source.Contains("using WoongBlog.Api.Infrastructure", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path))
            .OrderBy(path => path)
            .ToArray();

        Assert.Empty(violatingFiles);
    }

    [Fact]
    public void Public_Content_Query_Persistence_Filters_Search_Through_Query_Columns()
    {
        var queryStoreTypes = ApiAssembly.GetTypes()
            .Where(type => type.IsClass)
            .Where(type => typeof(IBlogQueryStore).IsAssignableFrom(type) || typeof(IWorkQueryStore).IsAssignableFrom(type))
            .Where(type => (type.Namespace ?? string.Empty).EndsWith(".Persistence", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(queryStoreTypes);

        var violations = queryStoreTypes
            .Select(type => new { Type = type, Source = ReadSourceForType(type) })
            .Where(candidate =>
                !candidate.Source.Contains(".SearchTitle.Contains(normalizedQuery)", StringComparison.Ordinal) ||
                !candidate.Source.Contains(".SearchText.Contains(normalizedQuery)", StringComparison.Ordinal) ||
                candidate.Source.Contains("ContentSearchText.", StringComparison.Ordinal))
            .Select(candidate => candidate.Type.FullName ?? candidate.Type.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(violations);
    }

    private static bool DependsOnDbContext(Type type)
    {
        var hasDbContextField = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Any(field => field.FieldType == typeof(WoongBlogDbContext));

        var hasDbContextConstructorParameter = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(WoongBlogDbContext));

        var hasDbContextMethodParameter = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(method => method.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(WoongBlogDbContext));

        return hasDbContextField || hasDbContextConstructorParameter || hasDbContextMethodParameter;
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
        return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(field => field.FieldType)
                .Concat(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .SelectMany(constructor => constructor.GetParameters())
                    .Select(parameter => parameter.ParameterType))
                .Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .SelectMany(method => method.GetParameters())
                    .Select(parameter => parameter.ParameterType))
                .Any(candidate => typeNames.Contains(candidate.Name));
    }

    private static bool IsMediatRRequestType(Type type)
    {
        return type.GetInterfaces().Any(candidate =>
            candidate.IsGenericType &&
            candidate.GetGenericTypeDefinition() == typeof(IRequest<>));
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

    private static string ReadSourceForType(Type type)
    {
        var sourceRoot = Path.Combine(FindRepositoryRoot(), "backend", "src", "WoongBlog.Api");
        var sourcePath = Directory.EnumerateFiles(sourceRoot, $"{type.Name}.cs", SearchOption.AllDirectories)
            .Single();

        return File.ReadAllText(sourcePath);
    }
}
