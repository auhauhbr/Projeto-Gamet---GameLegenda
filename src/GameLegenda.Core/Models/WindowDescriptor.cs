namespace GameLegenda.Core.Models;

public sealed record WindowDescriptor(
    nint Handle,
    string Title,
    int X,
    int Y,
    int Width,
    int Height);
