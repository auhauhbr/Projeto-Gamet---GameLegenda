using System.Text.Json;
using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public sealed class JsonTranslationCache : ITranslationCache
{
    private readonly string _filePath;
    private readonly Dictionary<string, TranslationEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonTranslationCache(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public bool TryGet(string sourceText, string sourceLanguage, string targetLanguage, out TranslationEntry entry)
    {
        return _entries.TryGetValue(CreateKey(sourceText, sourceLanguage, targetLanguage), out entry!);
    }

    public void Set(TranslationEntry entry)
    {
        _entries[CreateKey(entry.SourceText, entry.SourceLanguage, entry.TargetLanguage)] = entry;
        Save();
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var entries = JsonSerializer.Deserialize<List<TranslationEntry>>(json, _jsonOptions) ?? [];
            foreach (var entry in entries)
            {
                _entries[CreateKey(entry.SourceText, entry.SourceLanguage, entry.TargetLanguage)] = entry;
            }
        }
        catch
        {
            _entries.Clear();
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_entries.Values.OrderBy(entry => entry.SourceText), _jsonOptions));
    }

    private static string CreateKey(string sourceText, string sourceLanguage, string targetLanguage)
    {
        return $"{sourceLanguage.Trim().ToLowerInvariant()}:{targetLanguage.Trim().ToLowerInvariant()}:{TextNormalizer.NormalizeCacheKey(sourceText)}";
    }
}
