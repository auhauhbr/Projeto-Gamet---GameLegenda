namespace GameLegenda.Core.Models;

public sealed record CaptureRegion(
    string Id,
    string Name,
    int X,
    int Y,
    int Width,
    int Height,
    GameTextKind TextKind,
    bool IsEnabled = true,
    OverlayPlacement? Placement = null)
{
    public bool HasArea => Width > 0 && Height > 0;
}
