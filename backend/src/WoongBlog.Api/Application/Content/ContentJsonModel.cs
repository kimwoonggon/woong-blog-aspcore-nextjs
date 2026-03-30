using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WoongBlog.Api.Application.Content;

public sealed record RichTextContentDto(string? Html, string? Markdown)
{
    public string EditorText => !string.IsNullOrWhiteSpace(Html) ? Html : Markdown ?? string.Empty;
}

public sealed record PageContentDto(
    string? Html = null,
    JsonArray? Blocks = null,
    string? Headline = null,
    string? IntroText = null,
    string? ProfileImageUrl = null);

internal static class ContentJsonModel
{
    private static readonly Regex HtmlPattern = new("<(?:!DOCTYPE|html|body|p|div|h[1-6]|ul|ol|li|blockquote|code|pre|img|a|table|thead|tbody|tr|td|th|section|article|br|hr|span|strong|em|html-snippet|three-js-block)\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MarkdownPattern = new("(^|\\n)\\s*(#{1,6}\\s+|[-*+]\\s+|\\d+\\.\\s+|>\\s+|```|!\\[[^\\]]*\\]\\([^)]+\\)|\\[[^\\]]+\\]\\([^)]+\\))", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex HtmlTagPattern = new("<[^>]+>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SimpleWrapperOnlyPattern = new("^</?(?:p|div)(?:\\s[^>]*)?>|<br\\s*/?>$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryValidateRichTextJson(string json, out string error)
    {
        try
        {
            ParseRichText(json, "contentJson");
            error = string.Empty;
            return true;
        }
        catch (FormatException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static bool TryValidatePageJson(string json, out string error)
    {
        try
        {
            ParsePageContent(json, "contentJson");
            error = string.Empty;
            return true;
        }
        catch (FormatException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static bool TryValidateObjectJson(string json, string fieldName, out string error)
    {
        try
        {
            ParseObject(json, fieldName);
            error = string.Empty;
            return true;
        }
        catch (FormatException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static RichTextContentDto ParseRichText(string json, string fieldName)
    {
        var root = ParseObject(json, fieldName);
        var html = ReadOptionalString(root, "html", fieldName);
        var markdown = ReadOptionalString(root, "markdown", fieldName);

        if (string.IsNullOrWhiteSpace(html) && string.IsNullOrWhiteSpace(markdown))
        {
            throw new FormatException($"{fieldName} must contain a non-empty 'html' or 'markdown' string.");
        }

        return new RichTextContentDto(
            string.IsNullOrWhiteSpace(html) ? null : html,
            string.IsNullOrWhiteSpace(markdown) ? null : markdown);
    }

    public static PageContentDto ParsePageContent(string json, string fieldName)
    {
        var root = ParseObject(json, fieldName);
        var html = ReadOptionalString(root, "html", fieldName);
        var blocks = ReadOptionalArray(root, "blocks", fieldName);
        var headline = ReadOptionalString(root, "headline", fieldName);
        var introText = ReadOptionalString(root, "introText", fieldName);
        var profileImageUrl = ReadOptionalString(root, "profileImageUrl", fieldName);

        var hasHomeShape = !string.IsNullOrWhiteSpace(headline)
            || !string.IsNullOrWhiteSpace(introText)
            || !string.IsNullOrWhiteSpace(profileImageUrl);

        if (string.IsNullOrWhiteSpace(html) && blocks is null && !hasHomeShape)
        {
            throw new FormatException($"{fieldName} must contain either 'html', 'blocks', or home-page content fields.");
        }

        return new PageContentDto(
            string.IsNullOrWhiteSpace(html) ? null : html,
            blocks,
            string.IsNullOrWhiteSpace(headline) ? null : headline,
            string.IsNullOrWhiteSpace(introText) ? null : introText,
            string.IsNullOrWhiteSpace(profileImageUrl) ? null : profileImageUrl);
    }

    public static JsonObject ParseObject(string json, string fieldName)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node is JsonObject root)
            {
                return (JsonObject)root.DeepClone();
            }
        }
        catch (JsonException exception)
        {
            throw new FormatException($"{fieldName} must be valid JSON.", exception);
        }

        throw new FormatException($"{fieldName} must be a JSON object.");
    }

    public static string ExtractExcerptText(string json, string fieldName)
    {
        var content = ParseRichText(json, fieldName);
        if (!string.IsNullOrWhiteSpace(content.Markdown))
        {
            return StripMarkdown(content.Markdown);
        }

        var html = content.Html ?? string.Empty;
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var wrappedMarkdown = ExtractMarkdownFromSimpleHtml(html);
        if (!string.IsNullOrWhiteSpace(wrappedMarkdown))
        {
            return StripMarkdown(wrappedMarkdown);
        }

        if (LooksLikeHtml(html))
        {
            return html;
        }

        if (LooksLikeMarkdown(html))
        {
            return StripMarkdown(html);
        }

        return html;
    }

    public static string ExtractEditorText(string json, string fieldName)
    {
        return ParseRichText(json, fieldName).EditorText;
    }

    private static string? ReadOptionalString(JsonObject root, string propertyName, string fieldName)
    {
        if (!root.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return null;
        }

        return node switch
        {
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            _ => throw new FormatException($"{fieldName}.{propertyName} must be a string when present.")
        };
    }

    private static JsonArray? ReadOptionalArray(JsonObject root, string propertyName, string fieldName)
    {
        if (!root.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return null;
        }

        return node switch
        {
            JsonArray array => (JsonArray)array.DeepClone(),
            _ => throw new FormatException($"{fieldName}.{propertyName} must be an array when present.")
        };
    }

    private static bool LooksLikeHtml(string value) => HtmlPattern.IsMatch(value);

    private static bool LooksLikeMarkdown(string value) => MarkdownPattern.IsMatch(value);

    private static string StripMarkdown(string value)
    {
        var text = value;
        text = Regex.Replace(text, "```[\\s\\S]*?```", " ");
        text = Regex.Replace(text, "!\\[([^\\]]*)\\]\\([^)]+\\)", "$1");
        text = Regex.Replace(text, "\\[([^\\]]+)\\]\\([^)]+\\)", "$1");
        text = Regex.Replace(text, "(^|\\n)\\s*#{1,6}\\s*", " ");
        text = Regex.Replace(text, "(^|\\n)\\s*>\\s*", " ");
        text = Regex.Replace(text, "(^|\\n)\\s*(?:[-*+]\\s+|\\d+\\.\\s+)", " ");
        text = Regex.Replace(text, "[*_`~]", string.Empty);
        text = Regex.Replace(text, "\\s+", " ").Trim();
        return text;
    }

    private static string? ExtractMarkdownFromSimpleHtml(string html)
    {
        var tags = HtmlTagPattern.Matches(html).Select(match => match.Value.Trim());
        if (tags.Any(tag => !SimpleWrapperOnlyPattern.IsMatch(tag)))
        {
            return null;
        }

        var text = html
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase);

        text = Regex.Replace(text, "</p>\\s*<p(?:\\s[^>]*)?>", "\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "</div>\\s*<div(?:\\s[^>]*)?>", "\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "</?(?:p|div)(?:\\s[^>]*)?>", string.Empty, RegexOptions.IgnoreCase);
        text = text
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("&lt;", "<", StringComparison.OrdinalIgnoreCase)
            .Replace("&gt;", ">", StringComparison.OrdinalIgnoreCase)
            .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
            .Replace("&#39;", "'", StringComparison.OrdinalIgnoreCase)
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return LooksLikeMarkdown(text) ? text : null;
    }
}
