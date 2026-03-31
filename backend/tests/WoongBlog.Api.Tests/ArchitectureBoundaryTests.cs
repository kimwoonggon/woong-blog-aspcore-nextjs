using System.Reflection;
using WoongBlog.Api.Infrastructure.Persistence;

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
}
