using System.Text.Json;
using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public sealed class DeepLTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly Func<string?> _apiKeyProvider;
    private readonly Func<bool> _isEnabledProvider;

    public DeepLTranslationService(HttpClient httpClient, Func<string?> apiKeyProvider, Func<bool>? isEnabledProvider = null)
    {
        _httpClient = httpClient;
        _apiKeyProvider = apiKeyProvider;
        _isEnabledProvider = isEnabledProvider ?? (() => true);
    }

    public async Task<TranslationEntry> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        var normalized = TextNormalizer.NormalizeDisplayText(sourceText);
        var apiKey = _apiKeyProvider()?.Trim();
        if (!_isEnabledProvider() || string.IsNullOrWhiteSpace(apiKey))
        {
            return Unavailable(normalized, sourceLanguage, targetLanguage);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate");
            request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {apiKey}");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["text"] = normalized,
                ["source_lang"] = ToDeepLLanguage(sourceLanguage, isTarget: false),
                ["target_lang"] = ToDeepLLanguage(targetLanguage, isTarget: true)
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Unavailable(normalized, sourceLanguage, targetLanguage);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<DeepLResponse>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            var translated = payload?.Translations?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(translated))
            {
                return Unavailable(normalized, sourceLanguage, targetLanguage);
            }

            return new TranslationEntry(normalized, TextNormalizer.NormalizeDisplayText(translated), sourceLanguage, targetLanguage, "deepl", DateTimeOffset.UtcNow);
        }
        catch
        {
            return Unavailable(normalized, sourceLanguage, targetLanguage);
        }
    }

    private static TranslationEntry Unavailable(string sourceText, string sourceLanguage, string targetLanguage)
    {
        return new TranslationEntry(sourceText, sourceText, sourceLanguage, targetLanguage, "deepl-indisponivel", DateTimeOffset.UtcNow);
    }

    private static string ToDeepLLanguage(string language, bool isTarget)
    {
        var normalized = language.Trim().Replace('_', '-').ToUpperInvariant();
        if (normalized is "EN-US" or "EN-GB")
        {
            return isTarget ? normalized : "EN";
        }

        if (normalized is "PT" or "PT-BR")
        {
            return isTarget ? "PT-BR" : "PT";
        }

        return normalized.Split('-')[0];
    }

    private sealed class DeepLResponse
    {
        public List<DeepLTranslation>? Translations { get; set; }
    }

    private sealed class DeepLTranslation
    {
        public string? Text { get; set; }
    }
}
