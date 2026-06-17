using System.Text.Json;
using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public sealed class LibreTranslateTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly Func<string> _endpointProvider;
    private readonly Func<bool> _isEnabledProvider;

    public LibreTranslateTranslationService(HttpClient httpClient, Func<string>? endpointProvider = null, Func<bool>? isEnabledProvider = null)
    {
        _httpClient = httpClient;
        _endpointProvider = endpointProvider ?? (() => "http://127.0.0.1:5000/translate");
        _isEnabledProvider = isEnabledProvider ?? (() => true);
    }

    public async Task<TranslationEntry> TranslateAsync(string sourceText, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        var normalized = TextNormalizer.NormalizeDisplayText(sourceText);
        if (!_isEnabledProvider())
        {
            return Unavailable(normalized, sourceLanguage, targetLanguage);
        }

        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["q"] = normalized,
                ["source"] = ToLibreLanguage(sourceLanguage),
                ["target"] = ToLibreLanguage(targetLanguage),
                ["format"] = "text"
            });

            using var response = await _httpClient.PostAsync(_endpointProvider(), content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Unavailable(normalized, sourceLanguage, targetLanguage);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<LibreTranslateResponse>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            if (string.IsNullOrWhiteSpace(payload?.TranslatedText))
            {
                return Unavailable(normalized, sourceLanguage, targetLanguage);
            }

            return new TranslationEntry(normalized, TextNormalizer.NormalizeDisplayText(payload.TranslatedText), sourceLanguage, targetLanguage, "libretranslate", DateTimeOffset.UtcNow);
        }
        catch
        {
            return Unavailable(normalized, sourceLanguage, targetLanguage);
        }
    }

    private static TranslationEntry Unavailable(string sourceText, string sourceLanguage, string targetLanguage)
    {
        return new TranslationEntry(sourceText, sourceText, sourceLanguage, targetLanguage, "libretranslate-indisponivel", DateTimeOffset.UtcNow);
    }

    private static string ToLibreLanguage(string language)
    {
        return language.Trim().Replace('_', '-').ToLowerInvariant().Split('-')[0];
    }

    private sealed class LibreTranslateResponse
    {
        public string? TranslatedText { get; set; }
    }
}
