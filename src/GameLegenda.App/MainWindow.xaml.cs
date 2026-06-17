using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using GameLegenda.App.Services;
using GameLegenda.Core.Models;
using GameLegenda.Core.Services;

namespace GameLegenda.App;

public partial class MainWindow : Window
{
    private const int HotkeyCapture = 1001;
    private const int HotkeyOverlay = 1002;
    private const int HotkeyAdjustOverlay = 1003;
    private const int WmHotkey = 0x0312;
    private const int VkF8 = 0x77;
    private const int VkF9 = 0x78;
    private const int VkF10 = 0x79;

    private readonly ObservableCollection<WindowDescriptor> _windows = [];
    private readonly ObservableCollection<string> _events = [];
    private readonly GameProfile _profile = GameProfile.CreateDefault();
    private readonly IGlossaryService _glossary;
    private readonly ITranslationCache _cache;
    private readonly TranslationSettingsStore _translationSettingsStore = new();
    private readonly HttpClient _deepLHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly HttpClient _libreTranslateHttpClient = new() { Timeout = TimeSpan.FromSeconds(3) };
    private readonly Win32WindowCaptureService _captureService = new();
    private readonly WindowsNativeOcrService _nativeOcrService = new();
    private readonly WindowsPowerShellOcrService _powerShellOcrService = new();
    private readonly ScriptedTestOcrService _testOcrService;
    private readonly OverlayWindow _overlayWindow = new();
    private TranslationSettings _translationSettings;
    private ITranslationService _translationService;

    private HwndSource? _source;
    private CancellationTokenSource? _captureCancellation;
    private TestGameWindow? _testWindow;
    private bool _overlayEnabled = true;
    private bool _overlayAdjustmentMode;
    private string? _lastFingerprint;

    public MainWindow()
    {
        InitializeComponent();

        _glossary = new InMemoryGlossaryService(_profile.Glossary);
        var cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameLegenda",
            "translation-cache.json");
        _cache = new JsonTranslationCache(cachePath);
        _translationSettings = _translationSettingsStore.Load();
        _translationService = BuildTranslationService();

        _testOcrService = new ScriptedTestOcrService(() => _testWindow?.CurrentPhrase);

        WindowList.ItemsSource = _windows;
        EventList.ItemsSource = _events;

        Loaded += (_, _) =>
        {
            ApplyTranslationSettingsToUi();
            RefreshWindows();
        };
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
        NativeMethods.RegisterHotKey(handle, HotkeyCapture, 0, VkF8);
        NativeMethods.RegisterHotKey(handle, HotkeyOverlay, 0, VkF9);
        NativeMethods.RegisterHotKey(handle, HotkeyAdjustOverlay, 0, VkF10);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        StopCapture();
        _overlayWindow.Close();

        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.UnregisterHotKey(handle, HotkeyCapture);
        NativeMethods.UnregisterHotKey(handle, HotkeyOverlay);
        NativeMethods.UnregisterHotKey(handle, HotkeyAdjustOverlay);
        _source?.RemoveHook(WndProc);
        _deepLHttpClient.Dispose();
        _libreTranslateHttpClient.Dispose();
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return nint.Zero;
        }

        handled = true;
        if (wParam == HotkeyCapture)
        {
            ToggleCapture();
        }
        else if (wParam == HotkeyOverlay)
        {
            ToggleOverlay();
        }
        else if (wParam == HotkeyAdjustOverlay)
        {
            ToggleOverlayAdjustmentMode();
        }

        return nint.Zero;
    }

    private void OnOpenTestWindowClick(object sender, RoutedEventArgs e)
    {
        if (_testWindow is null)
        {
            _testWindow = new TestGameWindow();
            _testWindow.Closed += (_, _) => _testWindow = null;
            _testWindow.Show();
            AddEvent("Janela de teste aberta.");
        }
        else
        {
            _testWindow.Activate();
        }

        Dispatcher.InvokeAsync(RefreshWindows);
    }

    private void OnRefreshWindowsClick(object sender, RoutedEventArgs e) => RefreshWindows();

    private void OnToggleCaptureClick(object sender, RoutedEventArgs e) => ToggleCapture();

    private void OnToggleOverlayClick(object sender, RoutedEventArgs e) => ToggleOverlay();

    private void OnToggleAdjustOverlayClick(object sender, RoutedEventArgs e) => ToggleOverlayAdjustmentMode();

    private void OnApplyTranslationSettingsClick(object sender, RoutedEventArgs e)
    {
        _translationSettings.DeepLEnabled = DeepLEnabledBox.IsChecked == true;
        _translationSettings.DeepLApiKey = DeepLApiKeyBox.Password.Trim();
        _translationSettings.LibreTranslateEnabled = LibreEnabledBox.IsChecked == true;
        _translationSettings.LibreTranslateEndpoint = string.IsNullOrWhiteSpace(LibreEndpointBox.Text)
            ? "http://127.0.0.1:5000/translate"
            : LibreEndpointBox.Text.Trim();

        _translationSettingsStore.Save(_translationSettings);
        _translationService = BuildTranslationService();
        UpdateTranslationProviderStatus();
        AddEvent("Configuracao de traducao aplicada.");
    }

    private void OnWindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            StopCapture();
        }
    }

    private void RefreshWindows()
    {
        var selectedHandle = (WindowList.SelectedItem as WindowDescriptor)?.Handle;

        _windows.Clear();
        foreach (var window in _captureService.ListVisibleWindows().Where(window => !window.Title.Contains("GameLegenda v0.1", StringComparison.OrdinalIgnoreCase)))
        {
            _windows.Add(window);
        }

        var preferred = _windows.FirstOrDefault(window => selectedHandle == window.Handle)
            ?? _windows.FirstOrDefault(window => window.Title.Contains("Janela de Teste", StringComparison.OrdinalIgnoreCase))
            ?? _windows.FirstOrDefault();

        WindowList.SelectedItem = preferred;
        StatusText.Text = preferred is null ? "Nenhuma janela visivel encontrada." : "Pronto";
    }

    private void ToggleCapture()
    {
        if (_captureCancellation is null)
        {
            StartCapture();
        }
        else
        {
            StopCapture();
        }
    }

    private void StartCapture()
    {
        if (WindowList.SelectedItem is not WindowDescriptor selectedWindow)
        {
            StatusText.Text = "Selecione uma janela primeiro.";
            return;
        }

        _lastFingerprint = null;
        _captureCancellation = new CancellationTokenSource();
        CaptureButton.Content = "Parar captura (F8)";
        StatusText.Text = $"Capturando: {selectedWindow.Title}";
        AddEvent($"Captura iniciada em '{selectedWindow.Title}'.");

        _ = Task.Run(() => CaptureLoopAsync(selectedWindow.Handle, _captureCancellation.Token));
    }

    private void StopCapture()
    {
        if (_captureCancellation is null)
        {
            return;
        }

        _captureCancellation.Cancel();
        _captureCancellation = null;
        _overlayWindow.ClearTranslations();
        CaptureButton.Content = "Iniciar captura (F8)";
        StatusText.Text = "Captura parada.";
        AddEvent("Captura parada.");
    }

    private void ToggleOverlay()
    {
        _overlayEnabled = !_overlayEnabled;
        OverlayButton.Content = _overlayEnabled ? "Ocultar overlay (F9)" : "Mostrar overlay (F9)";

        if (!_overlayEnabled)
        {
            _overlayWindow.Hide();
            AddEvent("Overlay oculto.");
        }
        else
        {
            AddEvent("Overlay habilitado.");
        }
    }

    private void ToggleOverlayAdjustmentMode()
    {
        _overlayAdjustmentMode = !_overlayAdjustmentMode;
        _overlayWindow.SetAdjustmentMode(_overlayAdjustmentMode);
        AdjustOverlayButton.Content = _overlayAdjustmentMode ? "Concluir ajuste (F10)" : "Ajustar posição (F10)";
        AddEvent(_overlayAdjustmentMode
            ? "Ajuste do overlay ligado. Arraste a traducao para ajustar o deslocamento."
            : "Ajuste do overlay desligado.");
    }

    private async Task CaptureLoopAsync(nint windowHandle, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var currentWindow = _captureService.ListVisibleWindows().FirstOrDefault(window => window.Handle == windowHandle);
                if (currentWindow is null)
                {
                    await Dispatcher.InvokeAsync(() => StatusText.Text = "Janela capturada nao encontrada.");
                    await Task.Delay(900, cancellationToken);
                    continue;
                }

                if (NativeMethods.GetForegroundWindow() != windowHandle)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _overlayWindow.ClearTranslations();
                        StatusText.Text = $"Aguardando foco: {currentWindow.Title}";
                    });
                    _lastFingerprint = null;
                    await Task.Delay(900, cancellationToken);
                    continue;
                }

                await HideOverlayBeforeCaptureAsync(cancellationToken);

                var frame = await _captureService.CaptureAsync(windowHandle, null, cancellationToken);
                if (frame is null || frame.Fingerprint == _lastFingerprint)
                {
                    if (_overlayEnabled)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            _overlayWindow.SetTargetWindow(currentWindow);
                            _overlayWindow.RestoreLastTranslations();
                        });
                    }

                    await Task.Delay(900, cancellationToken);
                    continue;
                }

                _lastFingerprint = frame.Fingerprint;
                var ocrResult = await _powerShellOcrService.RecognizeAsync(frame, cancellationToken);
                var ocrDisplayName = _powerShellOcrService.DisplayName;

                if (!ocrResult.IsAvailable || ocrResult.Lines.Count == 0)
                {
                    var nativeResult = await _nativeOcrService.RecognizeAsync(frame, cancellationToken);
                    if (nativeResult.IsAvailable)
                    {
                        ocrResult = nativeResult;
                        ocrDisplayName = _nativeOcrService.DisplayName;
                    }
                }

                if ((!ocrResult.IsAvailable || ocrResult.Lines.Count == 0) && currentWindow.Title.Contains("Janela de Teste", StringComparison.OrdinalIgnoreCase))
                {
                    ocrResult = await _testOcrService.RecognizeAsync(frame, cancellationToken);
                    ocrDisplayName = _testOcrService.DisplayName;
                }

                if (!ocrResult.IsAvailable)
                {
                    await Dispatcher.InvokeAsync(() => OcrStatusText.Text = ocrResult.ErrorMessage ?? "OCR indisponivel.");
                    await Task.Delay(1200, cancellationToken);
                    continue;
                }

                if (ocrResult.Lines.Count == 0)
                {
                    await Dispatcher.InvokeAsync(() => OcrStatusText.Text = $"OCR: {ocrDisplayName}; nenhum texto encontrado neste frame.");
                    await Task.Delay(900, cancellationToken);
                    continue;
                }

                var translations = new List<OverlayTranslation>();
                foreach (var line in ocrResult.Lines)
                {
                    if (!IsCandidateText(line.Text))
                    {
                        continue;
                    }

                    var entry = await _translationService.TranslateAsync(line.Text, _profile.SourceLanguage, _profile.TargetLanguage, cancellationToken);
                    if (string.IsNullOrWhiteSpace(entry.TranslatedText) ||
                        string.Equals(entry.TranslatedText, entry.SourceText, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    translations.Add(new OverlayTranslation(
                        entry.SourceText,
                        entry.TranslatedText,
                        line.X,
                        line.Y,
                        line.Width,
                        line.Height,
                        _profile.CaptureRegions[0].Placement!,
                        _profile.Appearance,
                        DateTimeOffset.UtcNow));
                }

                translations = FilterOverlayTranslations(translations);
                if (translations.Count > 0)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        OcrStatusText.Text = $"OCR: {ocrDisplayName}; {translations.Count} texto(s) posicionados.";

                        if (_overlayEnabled)
                        {
                            _overlayWindow.SetTargetWindow(currentWindow);
                            _overlayWindow.Show(translations);
                        }
                    });
                }
                else
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _overlayWindow.ClearTranslations();
                        OcrStatusText.Text = $"OCR: {ocrDisplayName}; nenhum termo traduzivel encontrado.";
                    });
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    StatusText.Text = "Erro na captura.";
                    AddEvent(ex.Message);
                });
                await Task.Delay(1200, cancellationToken);
            }
        }
    }

    private void AddEvent(string message)
    {
        _events.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        while (_events.Count > 80)
        {
            _events.RemoveAt(_events.Count - 1);
        }
    }

    private ITranslationService BuildTranslationService()
    {
        var providers = new List<ITranslationService>
        {
            new DeepLTranslationService(
                _deepLHttpClient,
                () => _translationSettings.DeepLApiKey,
                () => _translationSettings.DeepLEnabled),
            new LibreTranslateTranslationService(
                _libreTranslateHttpClient,
                () => _translationSettings.LibreTranslateEndpoint,
                () => _translationSettings.LibreTranslateEnabled),
            new PlaceholderTranslationService(_glossary, _cache)
        };

        return new FallbackTranslationService(_glossary, _cache, providers);
    }

    private void ApplyTranslationSettingsToUi()
    {
        DeepLEnabledBox.IsChecked = _translationSettings.DeepLEnabled;
        DeepLApiKeyBox.Password = _translationSettings.DeepLApiKey;
        LibreEnabledBox.IsChecked = _translationSettings.LibreTranslateEnabled;
        LibreEndpointBox.Text = _translationSettings.LibreTranslateEndpoint;
        UpdateTranslationProviderStatus();
    }

    private void UpdateTranslationProviderStatus()
    {
        var deepLStatus = _translationSettings.DeepLEnabled && !string.IsNullOrWhiteSpace(_translationSettings.DeepLApiKey)
            ? "DeepL ligado"
            : "DeepL desligado";
        var libreStatus = _translationSettings.LibreTranslateEnabled
            ? $"Libre local: {_translationSettings.LibreTranslateEndpoint}"
            : "Libre local desligado";

        TranslationProviderStatusText.Text = $"Ordem: cache/glossario -> {deepLStatus} -> {libreStatus} -> dicionario local.";
    }

    private async Task HideOverlayBeforeCaptureAsync(CancellationToken cancellationToken)
    {
        if (!_overlayWindow.IsVisible)
        {
            return;
        }

        await Dispatcher.InvokeAsync(() => _overlayWindow.Hide());
        await Task.Delay(80, cancellationToken);
    }

    private static bool IsCandidateText(string? text)
    {
        var normalized = TextNormalizer.NormalizeDisplayText(text);
        if (normalized.Length < 2)
        {
            return false;
        }

        return normalized.Any(char.IsLetter);
    }

    private static List<OverlayTranslation> FilterOverlayTranslations(IReadOnlyList<OverlayTranslation> translations)
    {
        var accepted = new List<OverlayTranslation>();
        foreach (var translation in translations
                     .OrderByDescending(item => item.SourceText.Length)
                     .ThenByDescending(item => item.Width * item.Height))
        {
            if (accepted.Any(existing => OverlapsEnough(existing, translation)))
            {
                continue;
            }

            accepted.Add(translation);
            if (accepted.Count >= 28)
            {
                break;
            }
        }

        return accepted
            .OrderBy(item => item.Y)
            .ThenBy(item => item.X)
            .ToList();
    }

    private static bool OverlapsEnough(OverlayTranslation first, OverlayTranslation second)
    {
        var left = Math.Max(first.X, second.X);
        var top = Math.Max(first.Y, second.Y);
        var right = Math.Min(first.X + first.Width, second.X + second.Width);
        var bottom = Math.Min(first.Y + first.Height, second.Y + second.Height);
        if (right <= left || bottom <= top)
        {
            return false;
        }

        var intersection = (right - left) * (bottom - top);
        var firstArea = Math.Max(1, first.Width * first.Height);
        var secondArea = Math.Max(1, second.Width * second.Height);
        return intersection / Math.Min(firstArea, secondArea) > 0.45;
    }
}
