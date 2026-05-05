using System.Text.Json.Serialization;

namespace WoongBlog.Application.Modules.Content.Common;

public sealed record PublicContentBodyDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Html,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Markdown)
{
    public static PublicContentBodyDto FromStoredFields(string publicContentHtml, string publicContentMarkdown)
    {
        return new PublicContentBodyDto(
            Normalize(publicContentHtml),
            Normalize(publicContentMarkdown));
    }

    private static string? Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
