namespace GameLegenda.Core.Services;

public sealed class InMemoryGlossaryService : IGlossaryService
{
    private readonly Dictionary<string, string> _entries;

    public InMemoryGlossaryService(IReadOnlyDictionary<string, string>? seed = null)
    {
        _entries = new Dictionary<string, string>(seed ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, string> Entries => _entries;

    public bool TryTranslate(string sourceText, out string translatedText)
    {
        return _entries.TryGetValue(TextNormalizer.NormalizeDisplayText(sourceText), out translatedText!);
    }

    public void SetTerm(string sourceText, string translatedText)
    {
        var key = TextNormalizer.NormalizeDisplayText(sourceText);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _entries[key] = translatedText.Trim();
    }
}
