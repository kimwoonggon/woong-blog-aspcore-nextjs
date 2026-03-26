using System.Text.Json;

namespace Portfolio.Api.Application.Admin.Support;

internal static class AdminContentJson
{
    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();

    public static string ExtractHtml(string contentJson)
    {
        try
        {
            using var document = JsonDocument.Parse(contentJson);
            if (document.RootElement.TryGetProperty("html", out var html))
            {
                return html.GetString() ?? string.Empty;
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    public static JsonElement ParseObject(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.Clone()
                : EmptyObject;
        }
        catch
        {
            return EmptyObject;
        }
    }
}
