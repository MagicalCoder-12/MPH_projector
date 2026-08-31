using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Globalization;
using DrawingImage = System.Drawing.Image;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingColor = System.Drawing.Color;
using FormsScreen = System.Windows.Forms.Screen;
using MediaColor = System.Windows.Media.Color;
using System.Runtime.InteropServices;

namespace ChurchProjector;

public sealed class VideoProjectorWindow : Window
{
    private readonly Grid _root;
    private readonly Grid _canvas;
    private readonly MediaElement _video;
    private readonly Border _shade;
    private readonly TextBlock _lyrics;
    private readonly System.Windows.Controls.Image _logoControl;
    private bool _loop;
    private string? _videoPath;
    private string _lastSlideText = "";
    private string _aspectRatio = "16:9";
    private StageMode _stage = StageMode.Slide;
    private DrawingImage? _logoImage;
    private string? _cachedFontFamily;
    private System.Windows.Media.FontFamily? _cachedWpfFontFamily;
    private TextDecorationCollection? _cachedBoth;
    private static readonly SolidColorBrush _whiteBrush = new(System.Windows.Media.Colors.White);

    public VideoProjectorWindow(FormsScreen screen)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        Background = System.Windows.Media.Brushes.Black;
        Left = screen.Bounds.Left;
        Top = screen.Bounds.Top;
        Width = screen.Bounds.Width;
        Height = screen.Bounds.Height;
        Loaded += (_, _) => FitToScreen(screen);
        DpiChanged += (_, _) => FitToScreen(screen);
        SourceInitialized += (_, _) => FitToScreen(screen);

        _root = new Grid { Background = System.Windows.Media.Brushes.Black };
        _canvas = new Grid { HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Stretch };
        _canvas.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _canvas.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _video = new MediaElement { LoadedBehavior = MediaState.Manual, UnloadedBehavior = MediaState.Manual, Stretch = Stretch.UniformToFill, IsMuted = true, HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Stretch };
        _shade = new Border { Background = new SolidColorBrush(MediaColor.FromArgb(35, 0, 0, 0)) };
        _lyrics = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(80),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 5, ShadowDepth = 3, Opacity = .85 }
        };
        _canvas.Children.Add(_video);
        _canvas.Children.Add(_shade);
        _canvas.Children.Add(_lyrics);
        _logoControl = new System.Windows.Controls.Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Margin = new Thickness(80),
            Visibility = Visibility.Hidden
        };
        _canvas.Children.Add(_logoControl);
        _root.Children.Add(_canvas);
        Content = _root;

        SizeChanged += (_, _) => UpdateCanvasSize();
        KeyDown += (_, eventArgs) => { if (eventArgs.Key == Key.Escape) Close(); };
        _video.MediaOpened += (_, _) => _video.Play();
        _video.MediaEnded += (_, _) =>
        {
            if (_loop)
            {
                _video.Position = TimeSpan.Zero;
                _video.Play();
            }
            else
            {
                _video.Pause();
            }
        };
        Closed += (_, _) => _video.Stop();
    }

    public void SetSlide(string text, PresentationTheme theme)
    {
        var slideChanged = !string.Equals(_lastSlideText, text, StringComparison.Ordinal);
        _lastSlideText = text;
        _loop = theme.VideoLoop;
        _aspectRatio = theme.AspectRatio;
        _lyrics.Text = text;

        // Cache FontFamily to avoid recreating WPF FontFamily on every call
        if (!string.Equals(_cachedFontFamily, theme.FontFamily, StringComparison.Ordinal))
        {
            _cachedFontFamily = theme.FontFamily;
            _cachedWpfFontFamily = new System.Windows.Media.FontFamily(theme.FontFamily);
        }
        _lyrics.FontFamily = _cachedWpfFontFamily!;

        var baseSize = Math.Max(24, theme.FontSize * Math.Max(1, ActualWidth / 1280D));
        _lyrics.FontSize = theme.AutoFit && ActualWidth > 0
            ? FitFontSizeWpf(text, theme.FontFamily, theme.Bold, baseSize, _canvas.ActualWidth - 160, _canvas.ActualHeight - 160)
            : baseSize;
        _lyrics.FontWeight = theme.Bold ? FontWeights.Bold : FontWeights.Normal;
        _lyrics.FontStyle = theme.Italic ? FontStyles.Italic : FontStyles.Normal;

        // Cache TextDecorationCollection to avoid allocations
        if (theme.Underline && theme.Strikethrough)
        {
            _cachedBoth ??= new TextDecorationCollection { TextDecorations.Underline[0], TextDecorations.Strikethrough[0] };
            _lyrics.TextDecorations = _cachedBoth;
        }
        else if (theme.Underline)
            _lyrics.TextDecorations = TextDecorations.Underline;
        else if (theme.Strikethrough)
            _lyrics.TextDecorations = TextDecorations.Strikethrough;
        else
            _lyrics.TextDecorations = null;

        _lyrics.FontSize *= theme.Subscript || theme.Superscript ? .72 : 1;
        _lyrics.LineHeight = _lyrics.FontSize * Math.Max(.8, theme.LineSpacing);
        _lyrics.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        _lyrics.RenderTransform = new TranslateTransform(0, theme.Superscript ? -_lyrics.FontSize * .35 : theme.Subscript ? _lyrics.FontSize * .35 : 0);
        _lyrics.Background = theme.HighlightColor == DrawingColor.Transparent ? null : new SolidColorBrush(ToMediaColor(theme.HighlightColor));
        _lyrics.Foreground = new SolidColorBrush(ToMediaColor(theme.TextColor));
        _lyrics.TextAlignment = theme.Alignment switch
        {
            "Left" => TextAlignment.Left,
            "Right" => TextAlignment.Right,
            _ => TextAlignment.Center
        };
        _shade.Background = new SolidColorBrush(MediaColor.FromArgb((byte)Math.Clamp(35 - theme.Brightness, 0, 120), 0, 0, 0));
        UpdateCanvasSize();
        ApplyStage();
        if (slideChanged) AnimateText(theme.SlideTransition);

        if (string.IsNullOrWhiteSpace(theme.BackgroundVideoPath) || !File.Exists(theme.BackgroundVideoPath)) return;
        if (!string.Equals(_videoPath, theme.BackgroundVideoPath, StringComparison.OrdinalIgnoreCase))
        {
            _videoPath = theme.BackgroundVideoPath;
            _video.Source = new Uri(_videoPath, UriKind.Absolute);
        }
        _video.Play();
    }

    private void AnimateText(string transition)
    {
        _lyrics.BeginAnimation(UIElement.OpacityProperty, null);
        var translate = _lyrics.RenderTransform as TranslateTransform;
        translate?.BeginAnimation(TranslateTransform.YProperty, null);
        if (string.Equals(transition, "None", StringComparison.OrdinalIgnoreCase)) return;

        _lyrics.Opacity = 0;
        _lyrics.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)));
        if (translate is null) return;
        var offset = string.Equals(transition, "Zoom in", StringComparison.OrdinalIgnoreCase) ? -18D
            : string.Equals(transition, "Zoom out", StringComparison.OrdinalIgnoreCase) ? 18D
            : 0D;
        if (offset != 0D)
        {
            var target = translate.Y;
            translate.Y = target + offset;
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(target + offset, target, TimeSpan.FromMilliseconds(140)));
        }
    }

    public void SetStage(StageMode stage, DrawingImage? logo)
    {
        _stage = stage;
        _logoImage = logo;
        ApplyStage();
    }

    private void ApplyStage()
    {
        _lyrics.Visibility = _stage == StageMode.Slide ? Visibility.Visible : Visibility.Hidden;
        _video.Visibility = _stage == StageMode.Black ? Visibility.Hidden : Visibility.Visible;
        if (_stage == StageMode.Logo && _logoImage is DrawingBitmap bitmap)
        {
            _logoControl.Source = ToImageSource(bitmap);
            _logoControl.Visibility = Visibility.Visible;
        }
        else
        {
            _logoControl.Visibility = Visibility.Hidden;
        }
    }

    private static System.Windows.Media.ImageSource ToImageSource(DrawingBitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();
        try
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(handle);
        }
    }

    private void UpdateCanvasSize()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        if (string.Equals(_aspectRatio, "Current", StringComparison.OrdinalIgnoreCase))
        {
            _canvas.Width = ActualWidth;
            _canvas.Height = ActualHeight;
            _canvas.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            _canvas.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            _video.Width = ActualWidth;
            _video.Height = ActualHeight;
            _logoControl.Stretch = Stretch.UniformToFill;
            return;
        }
        _logoControl.Stretch = Stretch.Uniform;
        var values = _aspectRatio.Split(':');
        var ratio = values.Length == 2
                    && double.TryParse(values[0], out var width)
                    && double.TryParse(values[1], out var height)
                    && width > 0 && height > 0
            ? width / height
            : 16D / 9;
        var canvasWidth = ActualWidth;
        var canvasHeight = canvasWidth / ratio;
        if (canvasHeight > ActualHeight)
        {
            canvasHeight = ActualHeight;
            canvasWidth = canvasHeight * ratio;
        }
        _canvas.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        _canvas.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
        _canvas.Width = canvasWidth;
        _canvas.Height = canvasHeight;
        _video.Width = canvasWidth;
        _video.Height = canvasHeight;
    }

    private static double FitFontSizeWpf(string text, string family, bool bold, double baseSize, double availWidth, double availHeight)
    {
        if (availWidth <= 0 || availHeight <= 0) return baseSize;
        var weight = bold ? FontWeights.Bold : FontWeights.Normal;
        double Measure(double size)
        {
            var formatted = new FormattedText(text, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                new Typeface(new System.Windows.Media.FontFamily(family), FontStyles.Normal, weight, FontStretches.Normal),
                size, System.Windows.Media.Brushes.Black, 1);
            formatted.MaxTextWidth = availWidth;
            return formatted.Height;
        }
        var size = baseSize;
        if (Measure(size) > availHeight)
        {
            for (double s = size; s >= 12; s -= 1)
            {
                if (Measure(s) <= availHeight) { size = s; break; }
                size = s;
            }
        }
        else
        {
            for (double s = size; s <= 260; s += 1)
            {
                if (Measure(s) > availHeight) break;
                size = s;
            }
        }
        return size;
    }

    private static MediaColor ToMediaColor(DrawingColor color) => MediaColor.FromArgb(color.A, color.R, color.G, color.B);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly IntPtr HwndTopMost = new(-1);

    private void FitToScreen(FormsScreen screen)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        var bounds = screen.Bounds;
        SetWindowPos(handle, HwndTopMost, bounds.X, bounds.Y, bounds.Width, bounds.Height, SwpNoActivate | SwpShowWindow);
    }
}
