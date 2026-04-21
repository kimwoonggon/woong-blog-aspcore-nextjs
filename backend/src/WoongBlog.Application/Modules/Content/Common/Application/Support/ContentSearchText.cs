using System.Globalization;
using System.Text;

namespace WoongBlog.Api.Modules.Content.Common.Application.Support;

public static class ContentSearchText
{
    public static string BuildIndex(params string?[] values)
    {
        var normalizedValues = values
            .Select(Normalize)
            .Where(value => value.Length > 0);

        return string.Join(' ', normalizedValues);
    }

    public static bool ContainsNormalized(string? value, string? query)
    {
        var normalizedQuery = Normalize(query);
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            return true;
        }

        return Normalize(value).Contains(normalizedQuery, StringComparison.Ordinal);
    }

    public static bool AnyContainsNormalized(string? query, params string?[] values)
    {
        var normalizedQuery = Normalize(query);
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            return true;
        }

        return values.Any(value => Normalize(value).Contains(normalizedQuery, StringComparison.Ordinal));
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKC).ToLower(CultureInfo.InvariantCulture);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
