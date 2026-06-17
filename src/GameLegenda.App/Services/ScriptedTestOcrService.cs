using GameLegenda.Core.Models;
using GameLegenda.Core.Services;

namespace GameLegenda.App.Services;

public sealed class ScriptedTestOcrService : IOcrService
{
    private readonly Func<string?> _currentTextProvider;

    public ScriptedTestOcrService(Func<string?> currentTextProvider)
    {
        _currentTextProvider = currentTextProvider;
    }

    public string DisplayName => "Leitura simulada da janela de teste";

    public Task<OcrReadResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = _currentTextProvider();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new OcrReadResult([]));
        }

        return Task.FromResult(new OcrReadResult([
            new RecognizedText(text, 0, frame.Height * 0.65, frame.Width, frame.Height * 0.25, 1, GameTextKind.Dialogue)
        ]));
    }
}
