using System.Text.Json;
using FluentValidation;

namespace WoongBlog.Api.Application.Validation;

internal static class JsonValidationExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeJsonObject<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(BeJsonObject)
            .WithMessage("{PropertyName} must be a valid JSON object.");
    }

    private static bool BeJsonObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }
}
