using System.Runtime.InteropServices;
using System.Text;

namespace GameLegenda.App.Services;

internal static class NativeMethods
{
    public const int GwlExStyle = -20;
    public const int WsExTransparent = 0x00000020;
    public const int WsExLayered = 0x00080000;
    public const int WsExToolWindow = 0x00000080;
    public const int WsExNoActivate = 0x08000000;

    public delegate bool EnumWindowsProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(nint hWnd, out NativeRect lpRect);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(nint hWnd, int id);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct NativeRect
{
    public readonly int Left;
    public readonly int Top;
    public readonly int Right;
    public readonly int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}
