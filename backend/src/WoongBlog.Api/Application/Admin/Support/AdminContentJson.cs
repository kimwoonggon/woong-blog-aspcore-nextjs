using System.Text.Json;

namespace WoongBlog.Api.Application.Admin.Support;

internal static class AdminContentJson
{
    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();
    private static readonly System.Text.RegularExpressions.Regex HtmlPattern = new("<(?:!DOCTYPE|html|body|p|div|h[1-6]|ul|ol|li|blockquote|code|pre|img|a|table|thead|tbody|tr|td|th|section|article|br|hr|span|strong|em|html-snippet|three-js-block)\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex MarkdownPattern = new("(^|\\n)\\s*(#{1,6}\\s+|[-*+]\\s+|\\d+\\.\\s+|>\\s+|```|!\\[[^\\]]*\\]\\([^)]+\\)|\\[[^\\]]+\\]\\([^)]+\\))", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex HtmlTagPattern = new("<[^>]+>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex SimpleWrapperOnlyPattern = new("^</?(?:p|div)(?:\\s[^>]*)?>|<br\\s*/?>$", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string ExtractHtml(string contentJson)
    {
        try
        {
            using var document = JsonDocument.Parse(contentJson);
            if (document.RootElement.TryGetProperty("html", out var html))
            {
                return html.GetString() ?? string.Empty;
            }

            if (document.RootElement.TryGetProperty("markdown", out var markdown))
            {
                return markdown.GetString() ?? string.Empty;
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    public static string ExtractExcerptText(string contentJson)
    {
        try
        {
            using var document = JsonDocument.Parse(contentJson);
            var markdown = TryGetString(document.RootElement, "markdown");
            if (!string.IsNullOrWhiteSpace(markdown))
            {
                return StripMarkdown(markdown);
            }

            var html = TryGetString(document.RootElement, "html");
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
        catch
        {
            return string.Empty;
        }
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

    private static string TryGetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) ? value.GetString() ?? string.Empty : string.Empty;
    }

    private static bool LooksLikeHtml(string value)
    {
        return HtmlPattern.IsMatch(value);
    }

    private static bool LooksLikeMarkdown(string value)
    {
        return MarkdownPattern.IsMatch(value);
    }

    private static string StripMarkdown(string value)
    {
        var text = value;
        text = System.Text.RegularExpressions.Regex.Replace(text, "```[\\s\\S]*?```", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "!\\[([^\\]]*)\\]\\([^)]+\\)", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, "\\[([^\\]]+)\\]\\([^)]+\\)", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, "(^|\\n)\\s*#{1,6}\\s*", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "(^|\\n)\\s*>\\s*", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "(^|\\n)\\s*(?:[-*+]\\s+|\\d+\\.\\s+)", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "[*_`~]", string.Empty);
        text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ").Trim();
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

        text = System.Text.RegularExpressions.Regex.Replace(text, "</p>\\s*<p(?:\\s[^>]*)?>", "\n\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, "</div>\\s*<div(?:\\s[^>]*)?>", "\n\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, "</?(?:p|div)(?:\\s[^>]*)?>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
