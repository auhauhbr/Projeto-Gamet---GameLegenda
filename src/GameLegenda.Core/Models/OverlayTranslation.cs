namespace GameLegenda.Core.Models;

public sealed record OverlayTranslation(
    string SourceText,
    string TranslatedText,
    double X,
    double Y,
    double Width,
    double Height,
    OverlayPlacement Placement,
    OverlayAppearance Appearance,
    DateTimeOffset CreatedAt);
