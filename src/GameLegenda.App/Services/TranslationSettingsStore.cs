using System.IO;
using System.Text.Json;

namespace GameLegenda.App.Services;

public sealed class TranslationSettingsStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public TranslationSettingsStore()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameLegenda",
            "translation-settings.json");
    }

    public TranslationSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new TranslationSettings();
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<TranslationSettings>(json, _jsonOptions) ?? new TranslationSettings();
        }
        catch
        {
            return new TranslationSettings();
        }
    }

    public void Save(TranslationSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, _jsonOptions));
    }
}
