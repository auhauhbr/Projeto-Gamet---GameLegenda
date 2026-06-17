namespace GameLegenda.Core.Models;

public sealed record TranslationEntry(
    string SourceText,
    string TranslatedText,
    string SourceLanguage,
    string TargetLanguage,
    string Provider,
    DateTimeOffset CreatedAt);
