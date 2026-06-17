using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using GameLegenda.Core.Models;
using GameLegenda.Core.Services;

namespace GameLegenda.App.Services;

public sealed class Win32WindowCaptureService : IWindowCaptureService
{
    public IReadOnlyList<WindowDescriptor> ListVisibleWindows()
    {
        var windows = new List<WindowDescriptor>();

        NativeMethods.EnumWindows((handle, _) =>
        {
            if (!NativeMethods.IsWindowVisible(handle))
            {
                return true;
            }

            var titleLength = NativeMethods.GetWindowTextLength(handle);
            if (titleLength <= 0)
            {
                return true;
            }

            if (!NativeMethods.GetWindowRect(handle, out var rect) || rect.Width < 120 || rect.Height < 80)
            {
                return true;
            }

            var title = new StringBuilder(titleLength + 1);
            NativeMethods.GetWindowText(handle, title, title.Capacity);

            windows.Add(new WindowDescriptor(handle, title.ToString(), rect.Left, rect.Top, rect.Width, rect.Height));
            return true;
        }, nint.Zero);

        return windows
            .OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public Task<CapturedFrame?> CaptureAsync(nint windowHandle, CaptureRegion? region, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!NativeMethods.GetWindowRect(windowHandle, out var rect) || rect.Width <= 0 || rect.Height <= 0)
        {
            return Task.FromResult<CapturedFrame?>(null);
        }

        var captureX = rect.Left;
        var captureY = rect.Top;
        var width = rect.Width;
        var height = rect.Height;

        if (region is { HasArea: true })
        {
            captureX += region.X;
            captureY += region.Y;
            width = Math.Min(region.Width, Math.Max(1, rect.Width - region.X));
            height = Math.Min(region.Height, Math.Max(1, rect.Height - region.Y));
        }

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(captureX, captureY, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        }

        var area = new Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var pixels = new byte[stride * height];
            if (data.Stride > 0)
            {
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            }
            else
            {
                for (var row = 0; row < height; row++)
                {
                    var source = data.Scan0 + row * data.Stride;
                    Marshal.Copy(source, pixels, row * stride, stride);
                }
            }

            var fingerprint = CreateFingerprint(pixels);
            return Task.FromResult<CapturedFrame?>(new CapturedFrame(windowHandle, width, height, pixels, stride, DateTimeOffset.UtcNow, fingerprint));
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static string CreateFingerprint(byte[] pixels)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        const int targetSamples = 96;
        var rowLength = Math.Min(4096, pixels.Length);
        var stride = Math.Max(1, pixels.Length / targetSamples);

        for (var offset = 0; offset < pixels.Length; offset += stride)
        {
            var length = Math.Min(rowLength, pixels.Length - offset);
            hasher.AppendData(pixels.AsSpan(offset, length));
        }

        var hash = hasher.GetHashAndReset();
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}
