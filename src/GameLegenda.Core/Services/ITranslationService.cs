using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public interface ITranslationService
{
    Task<TranslationEntry> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken);
}
