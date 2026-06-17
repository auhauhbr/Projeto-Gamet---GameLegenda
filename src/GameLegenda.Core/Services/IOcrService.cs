using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public interface IOcrService
{
    string DisplayName { get; }
    Task<OcrReadResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken);
}
