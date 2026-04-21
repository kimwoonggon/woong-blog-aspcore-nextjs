using System.Reflection;
using System.Xml.Linq;
using MediatR;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.AI.Application;
using WoongBlog.Api.Modules.AI.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Architecture)]
public class ArchitectureBoundaryTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IBlogCommandStore).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(WoongBlogDbContext).Assembly;
    private static readonly string[] ProductionAssemblyNames =
    [
        "WoongBlog.Domain",
        "WoongBlog.Application",
        "WoongBlog.Infrastructure",
        "WoongBlog.Api"
    ];

    [Fact]
    public void Backend_IsSplitIntoExpectedProductionAssemblies()
    {
        var missingAssemblies = ProductionAssemblyNames
            .Where(assemblyName => TryLoadAssembly(assemblyName) is null)
            .ToArray();

        Assert.Empty(missingAssemblies);
    }

    [Fact]
    public void Production_ProjectReferences_FollowLayeredDirection()
    {
        var references = ProductionAssemblyNames
            .ToDictionary(
                assemblyName => assemblyName,
                assemblyName => GetWoongBlogProjectReferences(RequireAssembly(assemblyName)));

        Assert.Empty(references["WoongBlog.Domain"]);
        Assert.Equal(["WoongBlog.Domain"], references["WoongBlog.Application"]);
        Assert.Equal(["WoongBlog.Application", "WoongBlog.Domain"], references["WoongBlog.Infrastructure"]);
        Assert.DoesNotContain("WoongBlog.Domain", references["WoongBlog.Api"]);
        Assert.Contains("WoongBlog.Application", references["WoongBlog.Api"]);
        Assert.Contains("WoongBlog.Infrastructure", references["WoongBlog.Api"]);
        Assert.DoesNotContain("WoongBlog.Infrastructure", references["WoongBlog.Application"]);
        Assert.DoesNotContain("WoongBlog.Api", references["WoongBlog.Application"]);
        Assert.DoesNotContain("WoongBlog.Api", references["WoongBlog.Infrastructure"]);
    }

    [Fact]
    public void Application_DoesNotReference_InfrastructureOrApi()
    {
        var references = GetWoongBlogProjectReferences(RequireAssembly("WoongBlog.Application"));

        Assert.DoesNotContain("WoongBlog.Infrastructure", references);
        Assert.DoesNotContain("WoongBlog.Api", references);
    }

    [Fact]
    public void Domain_DoesNotReference_ApplicationInfrastructureOrApi()
    {
        var references = GetWoongBlogProjectReferences(RequireAssembly("WoongBlog.Domain"));

        Assert.DoesNotContain("WoongBlog.Application", references);
        Assert.DoesNotContain("WoongBlog.Infrastructure", references);
        Assert.DoesNotContain("WoongBlog.Api", references);
    }

    [Fact]
    public void Domain_DoesNotReference_FrameworkOrHigherLayerAssemblies()
    {
        var references = RequireAssembly("WoongBlog.Domain")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        var forbiddenReferences = references
            .Where(reference =>
                reference.StartsWith("WoongBlog.", StringComparison.Ordinal) ||
                reference.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
                reference.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
                reference == "MediatR")
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void Application_DoesNotReference_HttpPersistenceOrInfrastructureConcepts()
    {
        var references = RequireAssembly("WoongBlog.Application")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        var forbiddenReferences = references
            .Where(reference =>
                reference == "WoongBlog.Infrastructure" ||
                reference == "WoongBlog.Api" ||
                reference.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
                reference.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void Application_Source_DoesNotUseHttpResultsOrServiceLocator()
    {
        var applicationDirectory = Path.Combine(
            FindRepositoryRoot(),
            "backend",
            "src",
            "WoongBlog.Application");

        var disallowedTokens = new[]
        {
            "IResult",
            "Results.",
            "TypedResults.",
            "StatusCodes.",
            "HttpContext",
            "IFormFile",
            "IServiceScopeFactory",
            ".CreateScope(",
            ".CreateAsyncScope(",
            "GetRequiredService<",
            "GetRequiredService(",
        };

        var violatingFiles = FindSourceFilesContainingTokens(applicationDirectory, disallowedTokens);

        Assert.Empty(violatingFiles);
    }

    [Fact]
    public void Application_DoesNotExpose_AspNetCoreHttpResultTypes()
    {
        var violatingMembers = FindMembersUsingForbiddenTypes(ApplicationAssembly, IsAspNetCoreHttpType);

        Assert.Empty(violatingMembers);
    }

    [Fact]
    public void Application_DoesNotUse_ServiceScopeFactoryOrServiceLocator()
    {
        var applicationDirectory = Path.Combine(
            FindRepositoryRoot(),
            "backend",
            "src",
            "WoongBlog.Application");

        var violatingMembers = FindMembersUsingForbiddenTypes(ApplicationAssembly, IsServiceLocatorType);
        var violatingFiles = FindSourceFilesContainingTokens(
            applicationDirectory,
            [
                "IServiceScopeFactory",
                ".CreateScope(",
                ".CreateAsyncScope(",
                "GetRequiredService<",
                "GetRequiredService("
            ]);

        Assert.Empty(violatingMembers);
        Assert.Empty(violatingFiles);
    }

    [Fact]
    public void Application_ResultTypes_RemainHttpAgnostic()
    {
        var resultTypes = new[]
        {
            typeof(AiActionResult<>),
            typeof(AiActionStatus),
            typeof(WorkVideoResult<>),
            typeof(WorkVideoResultStatus)
        };
        var typesOutsideApplication = resultTypes
            .Where(type => type.Assembly != ApplicationAssembly)
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name)
            .ToArray();
        var violatingMembers = resultTypes
            .SelectMany(type => FindForbiddenTypeSurface(type, IsAspNetCoreHttpType))
            .OrderBy(name => name)
            .ToArray();

        Assert.Empty(typesOutsideApplication);
        Assert.Empty(violatingMembers);
    }

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
    public void Composition_Registration_Extensions_Exist_For_Approved_Boundaries()
    {
        var expectedTypes = new Dictionary<Assembly, string[]>
        {
            [ApiAssembly] =
            [
                "WoongBlog.Api.Common.ApiServiceCollectionExtensions",
                "WoongBlog.Api.Modules.Content.Pages.PagesModuleServiceCollectionExtensions",
                "WoongBlog.Api.Modules.Content.Blogs.BlogsModuleServiceCollectionExtensions",
                "WoongBlog.Api.Modules.Content.Works.WorksModuleServiceCollectionExtensions",
                "WoongBlog.Api.Modules.Site.SiteModuleServiceCollectionExtensions",
                "WoongBlog.Api.Modules.Composition.CompositionModuleServiceCollectionExtensions",
                "WoongBlog.Api.Modules.Identity.IdentityModuleServiceCollectionExtensions",
                "WoongBlog.Api.Modules.Media.MediaModuleServiceCollectionExtensions",
                "WoongBlog.Api.Modules.AI.AiModuleServiceCollectionExtensions",
            ],
            [ApplicationAssembly] =
            [
                "WoongBlog.Api.Application.ApplicationServiceCollectionExtensions",
            ],
            [InfrastructureAssembly] =
            [
                "WoongBlog.Api.Infrastructure.InfrastructureServiceCollectionExtensions",
            ],
        };

        var missingTypes = expectedTypes
            .SelectMany(pair => pair.Value.Where(typeName => pair.Key.GetType(typeName) is null))
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
        var persistenceTypes = InfrastructureAssembly.GetTypes()
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

        var violatingTypes = ApplicationAssembly.GetTypes()
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

        var productionAssemblies = new[] { ApiAssembly, ApplicationAssembly, InfrastructureAssembly };
        var stillPresent = removedTypes
            .Where(typeName => productionAssemblies.Any(assembly => assembly.GetType(typeName) is not null))
            .ToArray();

        Assert.Empty(stillPresent);
    }

    [Fact]
    public void WorkVideo_Application_Result_DoesNot_Use_AspNetCore_StatusCodes()
    {
        var workVideoApplicationDirectory = Path.Combine(
            FindRepositoryRoot(),
            "backend",
            "src",
            "WoongBlog.Application",
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
            "WoongBlog.Application",
            "Modules",
            "AI",
            "Application");

        var disallowedTokens = new[]
        {
            "IServiceScopeFactory",
            ".CreateScope(",
            ".CreateAsyncScope(",
            "GetRequiredService<",
            "GetRequiredService(",
        };

        var violatingFiles = FindSourceFilesContainingTokens(aiApplicationDirectory, disallowedTokens);

        Assert.Empty(violatingFiles);
    }

    [Fact]
    public void Content_Application_Abstractions_DoNot_Accept_MediatR_Request_Types()
    {
        var abstractionTypes = ApplicationAssembly.GetTypes()
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

    [Fact]
    public void WorkVideo_CommandStore_DoesNotExpose_BackgroundCleanupResponsibilities()
    {
        var commandStoreMethods = typeof(IWorkVideoCommandStore)
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();
        var cleanupStoreMethods = typeof(IWorkVideoCleanupStore)
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();
        var cleanupOnlyMethodNames = new[]
        {
            nameof(IWorkVideoCleanupStore.GetPendingCleanupJobsAsync),
            nameof(IWorkVideoCleanupStore.GetExpiredUploadSessionsAsync),
            nameof(IWorkVideoCleanupStore.EnqueueCleanupAsync)
        };

        Assert.DoesNotContain(commandStoreMethods, cleanupOnlyMethodNames.Contains);
        Assert.All(cleanupOnlyMethodNames, methodName => Assert.Contains(methodName, cleanupStoreMethods));
    }

    [Fact]
    public void Ai_Batch_AggregateBatchStore_IsRemoved()
    {
        var aggregateType = ApplicationAssembly.GetType("WoongBlog.Api.Modules.AI.Application.Abstractions.IAiBlogFixBatchStore");
        var sourceHits = FindSourceFilesContainingTokens(
            Path.Combine(FindRepositoryRoot(), "backend", "src"),
            ["IAiBlogFixBatchStore"]);

        Assert.Null(aggregateType);
        Assert.Empty(sourceHits);
    }

    [Fact]
    public void Api_ModuleRegistrations_DoNotReference_ConcreteInfrastructureAdapters()
    {
        var apiModulesDirectory = Path.Combine(
            FindRepositoryRoot(),
            "backend",
            "src",
            "WoongBlog.Api",
            "Modules");
        var registrationFiles = Directory.EnumerateFiles(apiModulesDirectory, "*ServiceCollectionExtensions.cs", SearchOption.AllDirectories);
        var forbiddenTokens = new[]
        {
            ".Persistence;",
            ".Storage;",
            ".Policies;",
            "WoongBlog.Api.Infrastructure.Ai",
            "WoongBlog.Api.Infrastructure.Storage"
        };
        var violatingFiles = registrationFiles
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return forbiddenTokens.Any(token => source.Contains(token, StringComparison.Ordinal));
            })
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path))
            .OrderBy(path => path)
            .ToArray();

        Assert.Empty(violatingFiles);
    }

    [Fact]
    public void UnitTestProject_DoesNotReference_Infrastructure_AspNetCore_Or_EfInMemory()
    {
        var projectPath = Path.Combine(
            FindRepositoryRoot(),
            "backend",
            "tests",
            "WoongBlog.Api.UnitTests",
            "WoongBlog.Api.UnitTests.csproj");
        var project = XDocument.Load(projectPath);
        var projectReferences = project.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();
        var frameworkReferences = project.Descendants("FrameworkReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();
        var packageReferences = project.Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(projectReferences, reference => reference.Contains("WoongBlog.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(frameworkReferences, reference => reference == "Microsoft.AspNetCore.App");
        Assert.DoesNotContain(packageReferences, reference => reference == "Microsoft.EntityFrameworkCore.InMemory");
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
        return GetReferencedTypes(type).Any(referencedType => IsTypeNamed(referencedType, typeNames));
    }

    private static string[] FindMembersUsingForbiddenTypes(Assembly assembly, Func<Type, bool> isForbiddenType)
    {
        return assembly.GetTypes()
            .SelectMany(type => FindForbiddenTypeSurface(type, isForbiddenType))
            .OrderBy(name => name)
            .ToArray();
    }

    private static IEnumerable<string> FindForbiddenTypeSurface(Type type, Func<Type, bool> isForbiddenType)
    {
        if (TypeContainsForbidden(type.BaseType, isForbiddenType))
        {
            yield return $"{type.FullName ?? type.Name} base type";
        }

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (TypeContainsForbidden(interfaceType, isForbiddenType))
            {
                yield return $"{type.FullName ?? type.Name} interface {FormatTypeName(interfaceType)}";
            }
        }

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (TypeContainsForbidden(field.FieldType, isForbiddenType))
            {
                yield return $"{type.FullName ?? type.Name}.{field.Name}";
            }
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (TypeContainsForbidden(property.PropertyType, isForbiddenType))
            {
                yield return $"{type.FullName ?? type.Name}.{property.Name}";
            }
        }

        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                if (TypeContainsForbidden(parameter.ParameterType, isForbiddenType))
                {
                    yield return $"{type.FullName ?? type.Name}..ctor({parameter.Name})";
                }
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (TypeContainsForbidden(method.ReturnType, isForbiddenType))
            {
                yield return $"{type.FullName ?? type.Name}.{method.Name} return";
            }

            foreach (var parameter in method.GetParameters())
            {
                if (TypeContainsForbidden(parameter.ParameterType, isForbiddenType))
                {
                    yield return $"{type.FullName ?? type.Name}.{method.Name}({parameter.Name})";
                }
            }
        }
    }

    private static IEnumerable<Type> GetReferencedTypes(Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            yield return field.FieldType;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            yield return property.PropertyType;
        }

        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            yield return method.ReturnType;
            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
    }

    private static bool TypeContainsForbidden(Type? type, Func<Type, bool> isForbiddenType)
    {
        if (type is null)
        {
            return false;
        }

        if (isForbiddenType(type))
        {
            return true;
        }

        if (type.HasElementType)
        {
            return TypeContainsForbidden(type.GetElementType(), isForbiddenType);
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        return (genericTypeDefinition != type && isForbiddenType(genericTypeDefinition)) ||
            type.GetGenericArguments().Any(argument => TypeContainsForbidden(argument, isForbiddenType));
    }

    private static string[] FindSourceFilesContainingTokens(string directory, IReadOnlyCollection<string> tokens)
    {
        var repositoryRoot = FindRepositoryRoot();

        return Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return tokens.Any(token => source.Contains(token, StringComparison.Ordinal));
            })
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .OrderBy(path => path)
            .ToArray();
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
        if (type.HasElementType)
        {
            return IsTypeNamed(type.GetElementType()!, typeNames);
        }

        if (type.IsGenericType)
        {
            return type.GetGenericArguments().Any(argument => IsTypeNamed(argument, typeNames));
        }

        return typeNames.Contains(type.Name);
    }

    private static bool IsAspNetCoreHttpType(Type type)
    {
        return (type.Namespace?.StartsWith("Microsoft.AspNetCore.Http", StringComparison.Ordinal) ?? false) ||
            type.FullName == "Microsoft.AspNetCore.Http.IResult" ||
            type.GetInterfaces().Any(candidate => candidate.FullName == "Microsoft.AspNetCore.Http.IResult");
    }

    private static bool IsServiceLocatorType(Type type)
    {
        return type.FullName is "System.IServiceProvider" or "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory";
    }

    private static string FormatTypeName(Type type)
    {
        return type.FullName ?? type.Name;
    }

    private static bool IsMediatRRequestType(Type type)
    {
        return typeof(IRequest).IsAssignableFrom(type) ||
            type.GetInterfaces().Any(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    private static Assembly RequireAssembly(string assemblyName)
    {
        return TryLoadAssembly(assemblyName)
            ?? throw new InvalidOperationException($"Could not load production assembly '{assemblyName}'.");
    }

    private static Assembly? TryLoadAssembly(string assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private static string[] GetWoongBlogProjectReferences(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(reference => reference.StartsWith("WoongBlog.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "backend", "src", "WoongBlog.Api")) &&
                Directory.Exists(Path.Combine(directory.FullName, "backend", "src", "WoongBlog.Application")) &&
                File.Exists(Path.Combine(directory.FullName, "package.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
