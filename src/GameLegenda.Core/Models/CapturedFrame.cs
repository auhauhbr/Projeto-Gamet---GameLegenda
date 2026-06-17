namespace GameLegenda.Core.Models;

public sealed record CapturedFrame(
    nint WindowHandle,
    int Width,
    int Height,
    byte[] BgraPixels,
    int Stride,
    DateTimeOffset CapturedAt,
    string Fingerprint);
