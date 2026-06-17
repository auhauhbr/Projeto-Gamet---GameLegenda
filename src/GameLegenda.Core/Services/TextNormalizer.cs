using System.Text.RegularExpressions;

namespace GameLegenda.Core.Services;

public static partial class TextNormalizer
{
    public static string NormalizeDisplayText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return WhitespaceRegex().Replace(text.Trim(), " ");
    }

    public static string NormalizeCacheKey(string? text)
    {
        return NormalizeDisplayText(text).ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
