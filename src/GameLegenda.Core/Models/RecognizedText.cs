namespace GameLegenda.Core.Models;

public sealed record RecognizedText(
    string Text,
    double X,
    double Y,
    double Width,
    double Height,
    double Confidence,
    GameTextKind Kind = GameTextKind.Unknown);
