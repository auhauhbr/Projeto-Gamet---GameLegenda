namespace GameLegenda.Core.Models;

public sealed record OverlayPlacement(
    OverlayPlacementMode Mode,
    double RelativeX,
    double RelativeY,
    double MaxWidth,
    bool FollowCapturedRegion);

public enum OverlayPlacementMode
{
    OverOriginalText,
    AboveOriginalText,
    BelowOriginalText,
    BottomCenter,
    TopCenter,
    Custom
}
