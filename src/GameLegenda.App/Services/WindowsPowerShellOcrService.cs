using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using GameLegenda.Core.Models;
using GameLegenda.Core.Services;

namespace GameLegenda.App.Services;

public sealed class WindowsPowerShellOcrService : IOcrService
{
    private readonly string _scriptPath;

    public WindowsPowerShellOcrService()
    {
        _scriptPath = Path.Combine(AppContext.BaseDirectory, "Assets", "RunWindowsOcr.ps1");
    }

    public string DisplayName => "Windows OCR via PowerShell";

    public async Task<OcrReadResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        if (!File.Exists(_scriptPath))
        {
            return OcrReadResult.Unavailable("Script local de OCR nao foi encontrado.");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), "GameLegenda", $"ocr-{Guid.NewGuid():N}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

        try
        {
            var regionMaps = SaveOptimizedOcrImage(frame, tempPath);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add("-NoProfile");
            process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
            process.StartInfo.ArgumentList.Add("Bypass");
            process.StartInfo.ArgumentList.Add("-File");
            process.StartInfo.ArgumentList.Add(_scriptPath);
            process.StartInfo.ArgumentList.Add("-ImagePath");
            process.StartInfo.ArgumentList.Add(tempPath);

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                return OcrReadResult.Unavailable($"OCR PowerShell falhou: {NormalizeSingleLine(error)}");
            }

            var lines = ParsePositionedTexts(output, regionMaps, frame.Width, frame.Height);

            return new OcrReadResult(lines);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OcrReadResult.Unavailable($"OCR PowerShell indisponivel: {ex.GetBaseException().Message}");
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static IReadOnlyList<OcrRegionMap> SaveOptimizedOcrImage(CapturedFrame frame, string path)
    {
        using var source = CreateBitmap(frame);
        var candidates = BuildCandidateRegions(source.Width, source.Height);
        const int scale = 3;
        var targetWidth = candidates.Max(region => region.Width) * scale;
        var targetHeight = candidates.Sum(region => region.Height * scale) + (candidates.Count - 1) * 24;
        var regionMaps = new List<OcrRegionMap>();

        using var target = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(target);
        graphics.Clear(Color.White);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;

        var y = 0;
        foreach (var region in candidates)
        {
            var destination = new Rectangle(0, y, region.Width * scale, region.Height * scale);
            graphics.DrawImage(source, destination, region, GraphicsUnit.Pixel);
            regionMaps.Add(new OcrRegionMap(region, y, scale));
            y += destination.Height + 24;
        }

        BoostContrast(target);
        target.Save(path, ImageFormat.Png);

        return regionMaps;
    }

    private static List<Rectangle> BuildCandidateRegions(int width, int height)
    {
        var regions = new List<Rectangle>
        {
            CreateSafeRegion(width, height, 0.02, 0.02, 0.96, 0.32),
            CreateSafeRegion(width, height, 0.02, 0.32, 0.96, 0.42),
            CreateSafeRegion(width, height, 0.02, 0.72, 0.96, 0.26)
        };

        return regions.Where(region => region.Width > 20 && region.Height > 20).ToList();
    }

    private static Rectangle CreateSafeRegion(int width, int height, double x, double y, double w, double h)
    {
        var left = Math.Clamp((int)(width * x), 0, Math.Max(0, width - 1));
        var top = Math.Clamp((int)(height * y), 0, Math.Max(0, height - 1));
        var regionWidth = Math.Clamp((int)(width * w), 1, width - left);
        var regionHeight = Math.Clamp((int)(height * h), 1, height - top);

        return new Rectangle(left, top, regionWidth, regionHeight);
    }

    private static Bitmap CreateBitmap(CapturedFrame frame)
    {
        var bitmap = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, frame.Width, frame.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (var row = 0; row < frame.Height; row++)
            {
                Marshal.Copy(frame.BgraPixels, row * frame.Stride, data.Scan0 + row * data.Stride, Math.Min(frame.Stride, Math.Abs(data.Stride)));
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }

    private static void BoostContrast(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var bytes = new byte[stride * bitmap.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            for (var row = 0; row < bitmap.Height; row++)
            {
                var rowStart = row * stride;
                for (var column = 0; column < bitmap.Width; column++)
                {
                    var i = rowStart + column * 3;
                    var blue = bytes[i];
                    var green = bytes[i + 1];
                    var red = bytes[i + 2];
                    var luminance = (red * 0.299) + (green * 0.587) + (blue * 0.114);

                    if (luminance > 165)
                    {
                        bytes[i] = 255;
                        bytes[i + 1] = 255;
                        bytes[i + 2] = 255;
                    }
                    else if (luminance < 95)
                    {
                        bytes[i] = 0;
                        bytes[i + 1] = 0;
                        bytes[i + 2] = 0;
                    }
                }
            }

            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static string NormalizeSingleLine(string text)
    {
        return string.Join(" ", text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static IReadOnlyList<RecognizedText> ParsePositionedTexts(string output, IReadOnlyList<OcrRegionMap> regionMaps, int frameWidth, int frameHeight)
    {
        var trimmed = output.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        List<OcrScriptItem>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<OcrScriptItem>>(trimmed, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return trimmed
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => new RecognizedText(line, 0, 0, frameWidth, frameHeight, 0.5, GameTextKind.Unknown))
                .ToList();
        }

        if (items is null || items.Count == 0)
        {
            return [];
        }

        var positioned = new List<RecognizedText>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Text))
            {
                continue;
            }

            var map = regionMaps.FirstOrDefault(candidate =>
                item.Y >= candidate.StackTop &&
                item.Y <= candidate.StackTop + candidate.Source.Height * candidate.Scale);

            if (map is null)
            {
                continue;
            }

            var x = map.Source.X + item.X / map.Scale;
            var y = map.Source.Y + (item.Y - map.StackTop) / map.Scale;
            var width = Math.Max(8, item.Width / map.Scale);
            var height = Math.Max(8, item.Height / map.Scale);

            if (x < 0 || y < 0 || x > frameWidth || y > frameHeight)
            {
                continue;
            }

            positioned.Add(new RecognizedText(item.Text, x, y, width, height, item.Kind == "line" ? 0.82 : 0.7, GameTextKind.Unknown));
        }

        return positioned
            .OrderBy(item => item.Y)
            .ThenBy(item => item.X)
            .ToList();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cancellation only.
        }
    }

    private sealed record OcrRegionMap(Rectangle Source, int StackTop, int Scale);

    private sealed class OcrScriptItem
    {
        public string Kind { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
