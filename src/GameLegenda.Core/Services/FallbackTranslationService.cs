using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public sealed class FallbackTranslationService : ITranslationService
{
    private static readonly HashSet<string> InvalidCacheProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "rascunho-local",
        "sem-traducao-local",
        "deepl-indisponivel",
        "libretranslate-indisponivel"
    };

    private readonly IGlossaryService _glossary;
    private readonly ITranslationCache _cache;
    private readonly IReadOnlyList<ITranslationService> _providers;

    public FallbackTranslationService(IGlossaryService glossary, ITranslationCache cache, IEnumerable<ITranslationService> providers)
    {
        _glossary = glossary;
        _cache = cache;
        _providers = providers.ToList();
    }

    public async Task<TranslationEntry> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = TextNormalizer.NormalizeDisplayText(sourceText);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new TranslationEntry(string.Empty, string.Empty, sourceLanguage, targetLanguage, "empty", DateTimeOffset.UtcNow);
        }

        if (_glossary.TryTranslate(normalized, out var glossaryTranslation))
        {
            return new TranslationEntry(normalized, glossaryTranslation, sourceLanguage, targetLanguage, "glossario", DateTimeOffset.UtcNow);
        }

        if (_cache.TryGet(normalized, sourceLanguage, targetLanguage, out var cached) && IsUseful(cached))
        {
            return cached;
        }

        TranslationEntry? lastEntry = null;
        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = await provider.TranslateAsync(normalized, sourceLanguage, targetLanguage, cancellationToken);
            lastEntry = entry;

            if (!IsUseful(entry))
            {
                continue;
            }

            _cache.Set(entry);
            return entry;
        }

        return lastEntry ?? new TranslationEntry(normalized, normalized, sourceLanguage, targetLanguage, "sem-traducao", DateTimeOffset.UtcNow);
    }

    private static bool IsUseful(TranslationEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.TranslatedText) ||
            entry.TranslatedText.StartsWith("[pt-BR]", StringComparison.OrdinalIgnoreCase) ||
            InvalidCacheProviders.Contains(entry.Provider))
        {
            return false;
        }

        return !string.Equals(
            TextNormalizer.NormalizeCacheKey(entry.SourceText),
            TextNormalizer.NormalizeCacheKey(entry.TranslatedText),
            StringComparison.OrdinalIgnoreCase);
    }
}
