using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public interface ITranslationCache
{
    bool TryGet(string sourceText, string sourceLanguage, string targetLanguage, out TranslationEntry entry);
    void Set(TranslationEntry entry);
}
