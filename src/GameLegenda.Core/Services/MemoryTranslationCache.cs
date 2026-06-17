using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public sealed class MemoryTranslationCache : ITranslationCache
{
    private readonly Dictionary<string, TranslationEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string sourceText, string sourceLanguage, string targetLanguage, out TranslationEntry entry)
    {
        return _entries.TryGetValue(CreateKey(sourceText, sourceLanguage, targetLanguage), out entry!);
    }

    public void Set(TranslationEntry entry)
    {
        _entries[CreateKey(entry.SourceText, entry.SourceLanguage, entry.TargetLanguage)] = entry;
    }

    private static string CreateKey(string sourceText, string sourceLanguage, string targetLanguage)
    {
        return $"{sourceLanguage.Trim().ToLowerInvariant()}:{targetLanguage.Trim().ToLowerInvariant()}:{TextNormalizer.NormalizeCacheKey(sourceText)}";
    }
}
