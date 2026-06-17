namespace GameLegenda.Core.Services;

public interface IGlossaryService
{
    IReadOnlyDictionary<string, string> Entries { get; }
    bool TryTranslate(string sourceText, out string translatedText);
    void SetTerm(string sourceText, string translatedText);
}
