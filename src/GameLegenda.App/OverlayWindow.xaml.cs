using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using GameLegenda.App.Services;
using GameLegenda.Core.Models;
using GameLegenda.Core.Services;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaFontFamily = System.Windows.Media.FontFamily;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;
using WpfCursors = System.Windows.Input.Cursors;
using WpfSize = System.Windows.Size;

namespace GameLegenda.App;

public partial class OverlayWindow : Window, IOverlayPresenter
{
    private WindowDescriptor? _targetWindow;
    private bool _isAdjustmentMode;
    private bool _isDragging;
    private WpfPoint _dragStart;
    private double _dragStartOffsetX;
    private double _dragStartOffsetY;
    private double _offsetX;
    private double _offsetY = 18;
    private IReadOnlyList<OverlayTranslation> _lastTranslations = [];

    public OverlayWindow()
    {
        InitializeComponent();
        RootCanvas.IsHitTestVisible = false;
        SourceInitialized += (_, _) => ApplyWindowStyles();
    }

    public void SetTargetWindow(WindowDescriptor targetWindow)
    {
        _targetWindow = targetWindow;
        Left = targetWindow.X;
        Top = targetWindow.Y;
        Width = Math.Max(1, targetWindow.Width);
        Height = Math.Max(1, targetWindow.Height);
        RootCanvas.Width = Width;
        RootCanvas.Height = Height;
    }

    public void SetAdjustmentMode(bool isEnabled)
    {
        _isAdjustmentMode = isEnabled;
        RootCanvas.IsHitTestVisible = isEnabled;
        Cursor = isEnabled ? WpfCursors.SizeAll : WpfCursors.Arrow;
        ApplyWindowStyles();
    }

    public void ClearTranslations()
    {
        _lastTranslations = [];
        RootCanvas.Children.Clear();
        Hide();
    }

    public void RestoreLastTranslations()
    {
        if (_lastTranslations.Count > 0)
        {
            Show(_lastTranslations);
        }
    }

    public void Show(IReadOnlyList<OverlayTranslation> translations)
    {
        _lastTranslations = translations;
        RootCanvas.Children.Clear();

        if (translations.Count == 0)
        {
            Hide();
            return;
        }

        foreach (var translation in translations)
        {
            AddTranslationText(translation);
        }

        if (!IsVisible)
        {
            Show();
        }
    }

    private void AddTranslationText(OverlayTranslation translation)
    {
        var textBlock = new TextBlock
        {
            Text = translation.TranslatedText,
            FontFamily = new MediaFontFamily(translation.Appearance.FontFamily),
            FontSize = translation.Appearance.FontSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = ParseBrush(translation.Appearance.TextColor, MediaBrushes.Gold),
            MaxWidth = Math.Min(translation.Appearance.MaxWidth, Math.Max(90, translation.Width + 140)),
            TextWrapping = TextWrapping.Wrap,
            IsHitTestVisible = _isAdjustmentMode,
            Effect = translation.Appearance.TextShadow
                ? new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 1.5,
                    BlurRadius = 2.5,
                    Opacity = 0.9
                }
                : null
        };

        textBlock.Measure(new WpfSize(textBlock.MaxWidth, double.PositiveInfinity));

        var left = translation.X + _offsetX;
        var top = translation.Y + translation.Height + _offsetY;
        var measuredWidth = Math.Max(40, textBlock.DesiredSize.Width);
        var measuredHeight = Math.Max(18, textBlock.DesiredSize.Height);

        Canvas.SetLeft(textBlock, Math.Clamp(left, 0, Math.Max(0, Width - measuredWidth - 4)));
        Canvas.SetTop(textBlock, Math.Clamp(top, 0, Math.Max(0, Height - measuredHeight - 4)));
        RootCanvas.Children.Add(textBlock);
    }

    private static MediaBrush ParseBrush(string color, MediaBrush fallback)
    {
        try
        {
            return (MediaBrush)new BrushConverter().ConvertFromString(color)!;
        }
        catch
        {
            return fallback;
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isAdjustmentMode)
        {
            return;
        }

        _isDragging = true;
        _dragStart = e.GetPosition(this);
        _dragStartOffsetX = _offsetX;
        _dragStartOffsetY = _offsetY;
        RootCanvas.CaptureMouse();
    }

    private void OnMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!_isAdjustmentMode || !_isDragging)
        {
            return;
        }

        var position = e.GetPosition(this);
        _offsetX = _dragStartOffsetX + position.X - _dragStart.X;
        _offsetY = _dragStartOffsetY + position.Y - _dragStart.Y;
        Show(_lastTranslations);
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isAdjustmentMode)
        {
            return;
        }

        _isDragging = false;
        RootCanvas.ReleaseMouseCapture();
    }

    private void ApplyWindowStyles()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == nint.Zero)
        {
            return;
        }

        var style = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
        style |= NativeMethods.WsExLayered | NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate;

        if (_isAdjustmentMode)
        {
            style &= ~NativeMethods.WsExTransparent;
        }
        else
        {
            style |= NativeMethods.WsExTransparent;
        }

        NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, style);
    }
}
