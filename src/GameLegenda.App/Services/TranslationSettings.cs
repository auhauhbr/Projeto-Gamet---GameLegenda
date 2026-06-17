namespace GameLegenda.App.Services;

public sealed class TranslationSettings
{
    public bool DeepLEnabled { get; set; }
    public string DeepLApiKey { get; set; } = string.Empty;
    public bool LibreTranslateEnabled { get; set; } = true;
    public string LibreTranslateEndpoint { get; set; } = "http://127.0.0.1:5000/translate";
}
