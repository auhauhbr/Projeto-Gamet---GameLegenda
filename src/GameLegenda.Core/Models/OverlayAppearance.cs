namespace GameLegenda.Core.Models;

public sealed record OverlayAppearance(
    string FontFamily,
    double FontSize,
    string TextColor,
    string BackgroundColor,
    double BackgroundOpacity,
    bool TextShadow,
    double MaxWidth)
{
    public static OverlayAppearance Default { get; } = new(
        "Segoe UI",
        18,
        "#FFFFD54A",
        "#00000000",
        0,
        true,
        220);
}
