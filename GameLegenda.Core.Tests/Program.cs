using GameLegenda.Core.Models;
using GameLegenda.Core.Services;

var tests = new (string Name, Action Body)[]
{
    ("normaliza espacos para exibicao", TextNormalizerCollapsesWhitespace),
    ("cache normaliza chaves por idioma", CacheUsesNormalizedKeys),
    ("glossario tem prioridade exata", GlossaryReturnsExactTerm),
    ("filtro ignora vazios numeros e repetidos", FilterSkipsNoiseAndRecentDuplicates),
    ("tradutor placeholder usa glossario antes do cache", TranslatorUsesGlossaryFirst),
    ("tradutor placeholder nao mostra prefixo pt-br", TranslatorDoesNotShowLanguagePrefix),
    ("tradutor placeholder traduz frase de dialogo", TranslatorHandlesDialoguePhrase),
    ("fallback usa cache antes dos providers", FallbackUsesCacheBeforeProviders),
    ("fallback pula provider indisponivel", FallbackSkipsUnavailableProvider)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL  {test.Name}");
        Console.WriteLine($"      {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? $"{tests.Length} testes passaram." : $"{failures} teste(s) falharam.");
return failures == 0 ? 0 : 1;

static void TextNormalizerCollapsesWhitespace()
{
    AssertEqual("Hello traveler", TextNormalizer.NormalizeDisplayText("  Hello\r\n\t traveler  "));
    AssertEqual("hello traveler", TextNormalizer.NormalizeCacheKey("Hello   Traveler"));
}

static void CacheUsesNormalizedKeys()
{
    var cache = new MemoryTranslationCache();
    var entry = new TranslationEntry("Iron Sword", "Espada de Ferro", "en", "pt-BR", "test", DateTimeOffset.UtcNow);

    cache.Set(entry);

    AssertTrue(cache.TryGet(" iron   sword ", "EN", "pt-br", out var cached));
    AssertEqual("Espada de Ferro", cached.TranslatedText);
}

static void GlossaryReturnsExactTerm()
{
    var glossary = new InMemoryGlossaryService();
    glossary.SetTerm("Health Potion", "Pocao de Vida");

    AssertTrue(glossary.TryTranslate(" health potion ", out var translated));
    AssertEqual("Pocao de Vida", translated);
}

static void FilterSkipsNoiseAndRecentDuplicates()
{
    var filter = new RecentTextFilterService(TimeSpan.FromSeconds(5));
    var now = DateTimeOffset.UtcNow;

    AssertFalse(filter.ShouldTranslate("", now));
    AssertFalse(filter.ShouldTranslate("125", now));
    AssertTrue(filter.ShouldTranslate("Quest Updated", now));
    AssertFalse(filter.ShouldTranslate(" quest   updated ", now.AddSeconds(1)));
    AssertTrue(filter.ShouldTranslate("Quest Updated", now.AddSeconds(6)));
}

static void TranslatorUsesGlossaryFirst()
{
    var glossary = new InMemoryGlossaryService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Iron Sword"] = "Espada de Ferro"
    });
    var cache = new MemoryTranslationCache();
    cache.Set(new TranslationEntry("Iron Sword", "Cache errado", "en", "pt-BR", "test", DateTimeOffset.UtcNow));

    var translator = new PlaceholderTranslationService(glossary, cache);
    var translated = translator.TranslateAsync("Iron Sword", "en", "pt-BR", CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("Espada de Ferro", translated.TranslatedText);
    AssertEqual("glossario", translated.Provider);
}

static void TranslatorDoesNotShowLanguagePrefix()
{
    var translator = new PlaceholderTranslationService(new InMemoryGlossaryService(), new MemoryTranslationCache());

    var known = translator.TranslateAsync("Items", "en", "pt-BR", CancellationToken.None).GetAwaiter().GetResult();
    var unknown = translator.TranslateAsync("Unmapped Thing", "en", "pt-BR", CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("Itens", known.TranslatedText);
    AssertFalse(known.TranslatedText.Contains("[pt-BR]", StringComparison.OrdinalIgnoreCase));
    AssertEqual("Unmapped Thing", unknown.TranslatedText);
    AssertFalse(unknown.TranslatedText.Contains("[pt-BR]", StringComparison.OrdinalIgnoreCase));
}

static void TranslatorHandlesDialoguePhrase()
{
    var translator = new PlaceholderTranslationService(new InMemoryGlossaryService(), new MemoryTranslationCache());

    var translated = translator.TranslateAsync(
        "I don't think there are any ports to the north. A ship can only dock at a port,",
        "en",
        "pt-BR",
        CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("Eu nao acho que exista nenhum porto ao norte. Um navio so pode atracar em um porto,", translated.TranslatedText);

    var bikke = translator.TranslateAsync(
        "Ye believe me...don't ye?",
        "en",
        "pt-BR",
        CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("Tu acredita em mim... nao acredita?", bikke.TranslatedText);
}

static void FallbackUsesCacheBeforeProviders()
{
    var glossary = new InMemoryGlossaryService();
    var cache = new MemoryTranslationCache();
    cache.Set(new TranslationEntry("Hello", "Ola do cache", "en", "pt-BR", "test-cache", DateTimeOffset.UtcNow));

    var provider = new FakeTranslationService(new TranslationEntry("Hello", "Ola do provider", "en", "pt-BR", "fake", DateTimeOffset.UtcNow));
    var fallback = new FallbackTranslationService(glossary, cache, [provider]);

    var translated = fallback.TranslateAsync("Hello", "en", "pt-BR", CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("Ola do cache", translated.TranslatedText);
    AssertEqual(0, provider.CallCount);
}

static void FallbackSkipsUnavailableProvider()
{
    var glossary = new InMemoryGlossaryService();
    var cache = new MemoryTranslationCache();
    var unavailable = new FakeTranslationService(new TranslationEntry("Hello", "Hello", "en", "pt-BR", "deepl-indisponivel", DateTimeOffset.UtcNow));
    var available = new FakeTranslationService(new TranslationEntry("Hello", "Ola", "en", "pt-BR", "libretranslate", DateTimeOffset.UtcNow));
    var fallback = new FallbackTranslationService(glossary, cache, [unavailable, available]);

    var translated = fallback.TranslateAsync("Hello", "en", "pt-BR", CancellationToken.None).GetAwaiter().GetResult();

    AssertEqual("Ola", translated.TranslatedText);
    AssertEqual(1, unavailable.CallCount);
    AssertEqual(1, available.CallCount);
}

static void AssertTrue(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("Esperado verdadeiro, recebido falso.");
    }
}

static void AssertFalse(bool condition)
{
    if (condition)
    {
        throw new InvalidOperationException("Esperado falso, recebido verdadeiro.");
    }
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Esperado '{expected}', recebido '{actual}'.");
    }
}

internal sealed class FakeTranslationService : ITranslationService
{
    private readonly TranslationEntry _entry;

    public FakeTranslationService(TranslationEntry entry)
    {
        _entry = entry;
    }

    public int CallCount { get; private set; }

    public Task<TranslationEntry> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_entry with { SourceText = sourceText, SourceLanguage = sourceLanguage, TargetLanguage = targetLanguage });
    }
}
