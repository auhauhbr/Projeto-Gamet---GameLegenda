using System.Text.RegularExpressions;
using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public sealed class PlaceholderTranslationService : ITranslationService
{
    private static readonly IReadOnlyDictionary<string, string> ExactPhrases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["I don't think there are any ports to the north. A ship can only dock at a port,"] = "Eu nao acho que exista nenhum porto ao norte. Um navio so pode atracar em um porto,",
        ["I dont think there are any ports to the north. A ship can only dock at a port,"] = "Eu nao acho que exista nenhum porto ao norte. Um navio so pode atracar em um porto,",
        ["you know."] = "sabe.",
        ["I don't think there are any ports to the north. A ship can only dock at a port, you know."] = "Eu nao acho que exista nenhum porto ao norte. Um navio so pode atracar em um porto, sabe.",
        ["I dont think there are any ports to the north. A ship can only dock at a port, you know."] = "Eu nao acho que exista nenhum porto ao norte. Um navio so pode atracar em um porto, sabe.",
        ["Ye believe me...don't ye?"] = "Tu acredita em mim... nao acredita?",
        ["Ye believe me...dont ye?"] = "Tu acredita em mim... nao acredita?",
        ["I plan to buckle down and be the hardest worker in town."] = "Eu pretendo me esforcar e ser o trabalhador mais dedicado da cidade."
    };

    private static readonly IReadOnlyDictionary<string, string> BuiltInTerms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Items"] = "Itens",
        ["Magic"] = "Magia",
        ["Equip"] = "Equipar",
        ["Status"] = "Estado",
        ["Order"] = "Ordem",
        ["Configuration"] = "Configuracao",
        ["Quick Save"] = "Salvar rapido",
        ["Save"] = "Salvar",
        ["Back"] = "Voltar",
        ["Use"] = "Usar",
        ["Key Items"] = "Itens-chave",
        ["Sort"] = "Ordenar",
        ["Staff"] = "Cajado",
        ["Knife"] = "Faca",
        ["Ether"] = "Eter",
        ["Antidote"] = "Antidoto",
        ["Echo Grass"] = "Erva do Eco",
        ["Remedy"] = "Remedio",
        ["Cottage"] = "Cabana",
        ["Hammer"] = "Martelo",
        ["Chain Mail"] = "Cota de Malha",
        ["Potion"] = "Pocao",
        ["Clothes"] = "Roupas",
        ["Hi-Potion"] = "Pocao Alta",
        ["Phoenix Down"] = "Pena de Fenix",
        ["Eye Drops"] = "Colirio",
        ["Gold Needle"] = "Agulha Dourada",
        ["Tent"] = "Tenda",
        ["Rapier"] = "Rapieira",
        ["Leather Armor"] = "Armadura de Couro",
        ["Leather Cap"] = "Capuz de Couro",
        ["Leather Shield"] = "Escudo de Couro",
        ["Close Menu"] = "Fechar menu",
        ["Confirm"] = "Confirmar",
        ["Time"] = "Tempo",
        ["Gil"] = "Gil"
    };

    private readonly IGlossaryService _glossary;
    private readonly ITranslationCache _cache;

    public PlaceholderTranslationService(IGlossaryService glossary, ITranslationCache cache)
    {
        _glossary = glossary;
        _cache = cache;
    }

    public Task<TranslationEntry> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = CleanOcrText(TextNormalizer.NormalizeDisplayText(sourceText));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult(new TranslationEntry(string.Empty, string.Empty, sourceLanguage, targetLanguage, "empty", DateTimeOffset.UtcNow));
        }

        if (_glossary.TryTranslate(normalized, out var glossaryTranslation))
        {
            return Task.FromResult(new TranslationEntry(normalized, glossaryTranslation, sourceLanguage, targetLanguage, "glossario", DateTimeOffset.UtcNow));
        }

        if (ExactPhrases.TryGetValue(NormalizePhraseKey(normalized), out var exactPhrase))
        {
            var exact = new TranslationEntry(normalized, exactPhrase, sourceLanguage, targetLanguage, "frase-local", DateTimeOffset.UtcNow);
            _cache.Set(exact);
            return Task.FromResult(exact);
        }

        if (BuiltInTerms.TryGetValue(normalized, out var builtInTranslation))
        {
            var builtIn = new TranslationEntry(normalized, builtInTranslation, sourceLanguage, targetLanguage, "dicionario-local", DateTimeOffset.UtcNow);
            _cache.Set(builtIn);
            return Task.FromResult(builtIn);
        }

        if (_cache.TryGet(normalized, sourceLanguage, targetLanguage, out var cached) &&
            !cached.TranslatedText.StartsWith("[pt-BR]", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(cached.Provider, "rascunho-local", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(cached.Provider, "sem-traducao-local", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(cached);
        }

        var entry = new TranslationEntry(
            normalized,
            normalized,
            sourceLanguage,
            targetLanguage,
            "sem-traducao-local",
            DateTimeOffset.UtcNow);

        return Task.FromResult(entry);
    }

    private static string CleanOcrText(string text)
    {
        var cleaned = text
            .Replace('\u2019', '\'')
            .Replace('\u2018', '\'');

        cleaned = Regex.Replace(cleaned, @"\s*[:\uFF1A]?\s*\d+\s*$", string.Empty);
        cleaned = Regex.Replace(cleaned, @"^[^\p{L}\p{N}]+|[^\p{L}\p{N}.,!?']+$", string.Empty);
        return TextNormalizer.NormalizeDisplayText(cleaned);
    }

    private static string NormalizePhraseKey(string text)
    {
        var normalized = TextNormalizer.NormalizeDisplayText(text)
            .Replace('\u2019', '\'')
            .Replace('\u2018', '\'');

        return Regex.Replace(normalized, @"\s*\.\.\.\s*", "...");
    }
}
