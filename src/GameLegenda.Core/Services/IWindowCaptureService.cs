using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public interface IWindowCaptureService
{
    IReadOnlyList<WindowDescriptor> ListVisibleWindows();
    Task<CapturedFrame?> CaptureAsync(nint windowHandle, CaptureRegion? region, CancellationToken cancellationToken);
}
