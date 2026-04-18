namespace WoongBlog.Api.Modules.Content.Common.Application.Support;

internal static class AdminContentText
{
    public static string Slugify(string title, string fallbackPrefix)
    {
        var slug = title.Trim().ToLowerInvariant().Replace(" ", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^\p{L}\p{N}-]+", string.Empty);
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? $"{fallbackPrefix}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" : slug;
    }

    public static string GenerateExcerpt(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var withoutMermaidBlocks = System.Text.RegularExpressions.Regex.Replace(
            html,
            "<mermaid-block\\b[\\s\\S]*?</mermaid-block>",
            " ",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var text = System.Text.RegularExpressions.Regex.Replace(withoutMermaidBlocks, "<[^>]*>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ").Trim();
        return text.Length <= 160 ? text : $"{text[..160].Trim()}...";
    }
}
