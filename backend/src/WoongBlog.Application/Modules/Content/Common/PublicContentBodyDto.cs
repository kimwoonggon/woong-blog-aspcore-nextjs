using System.Text.Json.Serialization;

namespace WoongBlog.Application.Modules.Content.Common;

public sealed record PublicContentBodyDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Html,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Markdown)
{
    public static PublicContentBodyDto FromStoredFields(string publicContentHtml, string publicContentMarkdown)
    {
        var markdown = Normalize(publicContentMarkdown);
        if (markdown is not null)
        {
            return new PublicContentBodyDto(null, markdown);
        }

        return new PublicContentBodyDto(Normalize(publicContentHtml), null);
    }

    private static string? Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
