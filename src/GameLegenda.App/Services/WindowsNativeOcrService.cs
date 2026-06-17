using System.Collections;
using System.Reflection;
using GameLegenda.Core.Models;
using GameLegenda.Core.Services;

namespace GameLegenda.App.Services;

public sealed class WindowsNativeOcrService : IOcrService
{
    public string DisplayName => "Windows.Media.Ocr";

    public async Task<OcrReadResult> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        try
        {
            var engineType = ResolveWinRtType("Windows.Media.Ocr.OcrEngine", "Windows.Media.Ocr", "Windows.Foundation", "Windows");
            var softwareBitmapType = ResolveWinRtType("Windows.Graphics.Imaging.SoftwareBitmap", "Windows.Graphics.Imaging", "Windows.Foundation", "Windows");
            var pixelFormatType = ResolveWinRtType("Windows.Graphics.Imaging.BitmapPixelFormat", "Windows.Graphics.Imaging", "Windows.Foundation", "Windows");
            var alphaModeType = ResolveWinRtType("Windows.Graphics.Imaging.BitmapAlphaMode", "Windows.Graphics.Imaging", "Windows.Foundation", "Windows");
            var dataWriterType = ResolveWinRtType("Windows.Storage.Streams.DataWriter", "Windows.Storage.Streams", "Windows.Foundation", "Windows");

            if (engineType is null || softwareBitmapType is null || pixelFormatType is null || alphaModeType is null || dataWriterType is null)
            {
                return OcrReadResult.Unavailable("OCR nativo do Windows nao esta disponivel neste runtime.");
            }

            var engine = engineType.GetMethod("TryCreateFromUserProfileLanguages", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
            if (engine is null)
            {
                return OcrReadResult.Unavailable("OCR nativo do Windows nao encontrou idiomas instalados.");
            }

            using var dataWriter = Activator.CreateInstance(dataWriterType) as IDisposable;
            if (dataWriter is null)
            {
                return OcrReadResult.Unavailable("Nao foi possivel preparar a imagem para o OCR nativo.");
            }

            dataWriterType.GetMethod("WriteBytes")?.Invoke(dataWriter, [frame.BgraPixels]);
            var buffer = dataWriterType.GetMethod("DetachBuffer")?.Invoke(dataWriter, null);
            if (buffer is null)
            {
                return OcrReadResult.Unavailable("Nao foi possivel converter a imagem para o OCR nativo.");
            }

            var pixelFormat = Enum.Parse(pixelFormatType, "Bgra8");
            var alphaMode = Enum.Parse(alphaModeType, "Premultiplied");
            var createCopy = softwareBitmapType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "CreateCopyFromBuffer" && method.GetParameters().Length == 5);

            if (createCopy is null)
            {
                return OcrReadResult.Unavailable("API de imagem do Windows OCR nao encontrada.");
            }

            using var softwareBitmap = createCopy.Invoke(null, [buffer, pixelFormat, frame.Width, frame.Height, alphaMode]) as IDisposable;
            if (softwareBitmap is null)
            {
                return OcrReadResult.Unavailable("Nao foi possivel criar bitmap para OCR nativo.");
            }

            var operation = engineType.GetMethod("RecognizeAsync")?.Invoke(engine, [softwareBitmap]);
            if (operation is null)
            {
                return OcrReadResult.Unavailable("OCR nativo nao iniciou o reconhecimento.");
            }

            var result = await AwaitWinRtOperationAsync(operation, cancellationToken);
            if (result is null)
            {
                return OcrReadResult.Unavailable("OCR nativo nao retornou resultado.");
            }

            var lines = ReadLines(result);
            return new OcrReadResult(lines);
        }
        catch (Exception ex)
        {
            return OcrReadResult.Unavailable($"OCR nativo indisponivel: {ex.GetBaseException().Message}");
        }
    }

    private static async Task<object?> AwaitWinRtOperationAsync(object operation, CancellationToken cancellationToken)
    {
        var operationType = operation.GetType();
        var statusProperty = operationType.GetProperty("Status");
        var getResultsMethod = operationType.GetMethod("GetResults");

        for (var attempt = 0; attempt < 240; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = statusProperty?.GetValue(operation)?.ToString();
            if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return getResultsMethod?.Invoke(operation, null);
            }

            if (string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Canceled", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            await Task.Delay(25, cancellationToken);
        }

        return null;
    }

    private static IReadOnlyList<RecognizedText> ReadLines(object result)
    {
        var rawLines = result.GetType().GetProperty("Lines")?.GetValue(result) as IEnumerable;
        if (rawLines is null)
        {
            return [];
        }

        var lines = new List<RecognizedText>();
        foreach (var rawLine in rawLines)
        {
            var text = rawLine?.GetType().GetProperty("Text")?.GetValue(rawLine)?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                lines.Add(new RecognizedText(text, 0, 0, 0, 0, 0.8, GameTextKind.Unknown));
            }
        }

        return lines;
    }

    private static Type? ResolveWinRtType(string fullName, params string[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var type = Type.GetType($"{fullName}, {assembly}, ContentType=WindowsRuntime", throwOnError: false);
            if (type is not null)
            {
                return type;
            }
        }

        return Type.GetType(fullName, throwOnError: false);
    }
}
