namespace GameLegenda.Core.Models;

public sealed record OcrReadResult(
    IReadOnlyList<RecognizedText> Lines,
    string? ErrorMessage = null)
{
    public bool IsAvailable => ErrorMessage is null;
    public static OcrReadResult Unavailable(string message) => new([], message);
}
