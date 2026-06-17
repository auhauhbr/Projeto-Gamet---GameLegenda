using System.Text.RegularExpressions;

namespace GameLegenda.Core.Services;

public sealed partial class RecentTextFilterService : ITextFilterService
{
    private readonly TimeSpan _duplicateWindow;
    private readonly Dictionary<string, DateTimeOffset> _recentTexts = new(StringComparer.OrdinalIgnoreCase);

    public RecentTextFilterService(TimeSpan? duplicateWindow = null)
    {
        _duplicateWindow = duplicateWindow ?? TimeSpan.FromSeconds(2);
    }

    public bool ShouldTranslate(string? text, DateTimeOffset seenAt)
    {
        var normalized = TextNormalizer.NormalizeDisplayText(text);
        if (normalized.Length < 2 || NumberOnlyRegex().IsMatch(normalized))
        {
            return false;
        }

        var key = TextNormalizer.NormalizeCacheKey(normalized);
        if (_recentTexts.TryGetValue(key, out var lastSeen) && seenAt - lastSeen < _duplicateWindow)
        {
            return false;
        }

        _recentTexts[key] = seenAt;
        return true;
    }

    [GeneratedRegex(@"^[\d\s.,:/+\-%]+$")]
    private static partial Regex NumberOnlyRegex();
}
