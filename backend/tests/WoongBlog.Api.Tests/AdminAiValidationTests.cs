using System.Reflection;
using WoongBlog.Api.Endpoints;

namespace WoongBlog.Api.Tests;

public class AdminAiValidationTests
{
    [Fact]
    public void SelectedMode_RequiresIds()
    {
        var result = Validate(null, false);

        Assert.False(ReadBool(result, "IsValid"));
    }

    [Fact]
    public void AllMode_AllowsMissingIds()
    {
        var result = Validate(null, true);

        Assert.True(ReadBool(result, "IsValid"));
        Assert.Empty((IReadOnlyList<Guid>)ReadValue(result, "BlogIds")!);
    }

    [Fact]
    public void DuplicateIds_AreDeduplicated()
    {
        var id = Guid.NewGuid();
        var result = Validate([id, id], false);

        Assert.True(ReadBool(result, "IsValid"));
        Assert.Single((IReadOnlyList<Guid>)ReadValue(result, "BlogIds")!);
    }

    [Fact]
    public void EmptyGuidIds_AreRejected_WhenNothingValidRemains()
    {
        var result = Validate([Guid.Empty], false);

        Assert.False(ReadBool(result, "IsValid"));
    }

    private static dynamic Validate(IReadOnlyList<Guid>? ids, bool all)
    {
        var type = typeof(AdminAiEndpoints).Assembly.GetType("WoongBlog.Api.Endpoints.AdminAiSelectionValidator")!;
        var method = type.GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
        return method.Invoke(null, [ids, all])!;
    }

    private static bool ReadBool(object instance, string propertyName)
        => (bool)ReadValue(instance, propertyName)!;

    private static object? ReadValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(instance);
}
