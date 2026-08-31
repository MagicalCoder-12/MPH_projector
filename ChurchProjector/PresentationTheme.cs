using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace ChurchProjector;

public sealed class PresentationTheme
{
    public string FontFamily { get; set; } = "Segoe UI";
    public float FontSize { get; set; } = 56;
    public bool Bold { get; set; } = true;
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool Strikethrough { get; set; }
    public bool Subscript { get; set; }
    public bool Superscript { get; set; }
    public Color TextColor { get; set; } = Color.White;
    public Color HighlightColor { get; set; } = Color.Transparent;
    public string Alignment { get; set; } = "Centre";
    public float LineSpacing { get; set; } = 1.0f;
    public Color BackgroundColor { get; set; } = Color.FromArgb(22, 34, 52);
    public Image? BackgroundImage { get; set; }
    public Guid? BackgroundAssetId { get; set; }
    public string? BackgroundVideoPath { get; set; }
    public bool VideoLoop { get; set; } = true;
    public int Brightness { get; set; }
    public string AspectRatio { get; set; } = "16:9";
    public string SlideTransition { get; set; } = "Cross fade";
    public bool AutoFit { get; set; }
}

public enum StageMode
{
    Slide,
    Background,
    Black,
    Logo
}

public sealed class SlideCanvas : Control
{
    private readonly System.Windows.Forms.Timer _transitionTimer = new() { Interval = 16 };
    private string _slideText = "";
    private float _transitionProgress = 1F;
    private string _transition = "None";
    public PresentationTheme? Theme { get; set; }
    public string SlideText
    {
        get => _slideText;
        set
        {
            if (string.Equals(_slideText, value, StringComparison.Ordinal)) return;
            var animate = Theme is not null && !string.Equals(Theme.SlideTransition, "None", StringComparison.OrdinalIgnoreCase) && _slideText.Length > 0;
            _slideText = value;
            if (animate)
            {
                _transition = Theme!.SlideTransition;
                _transitionProgress = 0F;
                _transitionTimer.Start();
            }
            else _transitionProgress = 1F;
            Invalidate();
        }
    }
    public StageMode Stage { get; set; } = StageMode.Slide;
    public Image? LogoImage { get; set; }

    public SlideCanvas()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        _transitionTimer.Tick += (_, _) =>
        {
            _transitionProgress = Math.Min(1F, _transitionProgress + .14F);
            if (_transitionProgress >= 1F) _transitionTimer.Stop();
            Invalidate();
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var theme = Theme;
        if (theme is null) return;
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        var canvas = FitAspect(ClientRectangle, theme.AspectRatio);
        using var surround = new SolidBrush(Color.FromArgb(25, 31, 40));
        e.Graphics.FillRectangle(surround, ClientRectangle);

        if (Stage == StageMode.Black)
        {
            e.Graphics.FillRectangle(Brushes.Black, canvas);
            return;
        }
        DrawBackground(e.Graphics, canvas, theme);
        if (Stage == StageMode.Logo && LogoImage is not null)
        {
            DrawLogo(e.Graphics, canvas, LogoImage);
            return;
        }
        if (Stage == StageMode.Slide)
        {
            var state = e.Graphics.Save();
            if (string.Equals(_transition, "Zoom in", StringComparison.OrdinalIgnoreCase) || string.Equals(_transition, "Zoom out", StringComparison.OrdinalIgnoreCase))
            {
                var scale = string.Equals(_transition, "Zoom in", StringComparison.OrdinalIgnoreCase)
                    ? .92F + .08F * _transitionProgress
                    : 1.08F - .08F * _transitionProgress;
                e.Graphics.TranslateTransform(canvas.X + canvas.Width / 2F, canvas.Y + canvas.Height / 2F);
                e.Graphics.ScaleTransform(scale, scale);
                e.Graphics.TranslateTransform(-(canvas.X + canvas.Width / 2F), -(canvas.Y + canvas.Height / 2F));
            }
            DrawText(e.Graphics, canvas, SlideText, theme, _transitionProgress);
            e.Graphics.Restore(state);
        }
    }

    public static void DrawSlide(Graphics graphics, Rectangle canvas, string text, PresentationTheme theme)
    {
        DrawBackground(graphics, canvas, theme);
        DrawText(graphics, canvas, text, theme);
    }

    private static void DrawText(Graphics graphics, Rectangle canvas, string text, PresentationTheme theme, float opacity = 1F)
    {
        using var shade = new SolidBrush(Color.FromArgb(35, Color.Black));
        graphics.FillRectangle(shade, canvas);

        var style = FontStyle.Regular;
        if (theme.Bold) style |= FontStyle.Bold;
        if (theme.Italic) style |= FontStyle.Italic;
        if (theme.Underline) style |= FontStyle.Underline;
        if (theme.Strikethrough) style |= FontStyle.Strikeout;
        
        var availableWidth = Math.Max(40, canvas.Width - (canvas.Width * 16 / 100));
        var textArea = new RectangleF(canvas.X + canvas.Width * .08F, canvas.Y + canvas.Height * .12F, availableWidth, canvas.Height * .76F);
        var baseSize = Math.Max(12, canvas.Width * theme.FontSize / 1280F);
        var fontSize = theme.AutoFit ? FitFontSize(graphics, text, textArea, theme.FontFamily, style, baseSize) : baseSize;
        if (theme.Subscript || theme.Superscript) fontSize *= .72F;
        using var font = new Font(theme.FontFamily, fontSize, style, GraphicsUnit.Pixel);
        using var format = new StringFormat 
        { 
            LineAlignment = StringAlignment.Near,
            Alignment = theme.Alignment switch { "Left" => StringAlignment.Near, "Right" => StringAlignment.Far, "Justify" => StringAlignment.Center, _ => StringAlignment.Center }, 
            Trimming = StringTrimming.Word 
        };
        using var shadow = new SolidBrush(Color.FromArgb((int)(190 * opacity), Color.Black));
        using var brush = new SolidBrush(Color.FromArgb((int)(255 * opacity), theme.TextColor));
        var lines = text.Split(Environment.NewLine, StringSplitOptions.None);
        var lineHeight = font.GetHeight(graphics) * Math.Max(.8F, theme.LineSpacing);
        var totalHeight = lineHeight * lines.Length;
        var y = textArea.Y + (textArea.Height - totalHeight) / 2F;
        if (theme.Superscript) y -= fontSize * .35F;
        if (theme.Subscript) y += fontSize * .35F;
        foreach (var line in lines)
        {
            var lineArea = new RectangleF(textArea.X, y, textArea.Width, lineHeight);
            if (theme.HighlightColor != Color.Transparent && !string.IsNullOrWhiteSpace(line))
            {
                var measured = graphics.MeasureString(line, font);
                var width = Math.Min(measured.Width, textArea.Width);
                var x = format.Alignment switch
                {
                    StringAlignment.Far => textArea.Right - width,
                    StringAlignment.Center => textArea.X + (textArea.Width - width) / 2F,
                    _ => textArea.X
                };
                using var highlight = new SolidBrush(theme.HighlightColor);
                graphics.FillRectangle(highlight, x - 3F, y + 2F, width + 6F, Math.Max(2F, font.GetHeight(graphics)));
            }
            graphics.DrawString(line, font, shadow, new RectangleF(lineArea.X + 3F, lineArea.Y + 4F, lineArea.Width, lineArea.Height), format);
            graphics.DrawString(line, font, brush, lineArea, format);
            y += lineHeight;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _transitionTimer.Dispose();
        base.Dispose(disposing);
    }

    private static float FitFontSize(Graphics graphics, string text, RectangleF area, string family, FontStyle style, float startSize)
    {
        float size = Math.Max(12, startSize);
        float Measure(float s)
        {
            using var font = new Font(family, s, style, GraphicsUnit.Pixel);
            return graphics.MeasureString(text, font, (int)area.Width).Height;
        }
        if (Measure(size) > area.Height)
        {
            for (float s = size; s >= 12; s -= 1F)
            {
                if (Measure(s) <= area.Height) { size = s; break; }
                size = s;
            }
        }
        else
        {
            for (float s = size; s <= 240; s += 1F)
            {
                if (Measure(s) > area.Height) break;
                size = s;
            }
        }
        return size;
    }

    private static void DrawLogo(Graphics graphics, Rectangle canvas, Image logo)
    {
        var scale = Math.Max((float)canvas.Width / logo.Width, (float)canvas.Height / logo.Height);
        var width = (int)(logo.Width * scale);
        var height = (int)(logo.Height * scale);
        var x = canvas.X + (canvas.Width - width) / 2;
        var y = canvas.Y + (canvas.Height - height) / 2;
        graphics.DrawImage(logo, x, y, width, height);
    }

    private static void DrawBackground(Graphics graphics, Rectangle canvas, PresentationTheme theme)
    {
        using var background = new SolidBrush(ApplyBrightness(theme.BackgroundColor, theme.Brightness));
        graphics.FillRectangle(background, canvas);
        if (theme.BackgroundImage is null) return;

        var destination = Cover(canvas, theme.BackgroundImage.Size);
        using var attributes = new ImageAttributes();
        var adjustment = 1F + theme.Brightness / 100F;
        attributes.SetColorMatrix(new ColorMatrix(new[]
        {
            new[] { adjustment, 0F, 0F, 0F, 0F }, new[] { 0F, adjustment, 0F, 0F, 0F },
            new[] { 0F, 0F, adjustment, 0F, 0F }, new[] { 0F, 0F, 0F, 1F, 0F }, new[] { 0F, 0F, 0F, 0F, 1F }
        }));
        graphics.DrawImage(theme.BackgroundImage, destination, 0, 0, theme.BackgroundImage.Width, theme.BackgroundImage.Height, GraphicsUnit.Pixel, attributes);
    }

    private static Rectangle FitAspect(Rectangle container, string ratio)
    {
        if (string.Equals(ratio, "Current", StringComparison.OrdinalIgnoreCase))
            return container;
        var values = ratio.Split(':');
        var target = values.Length == 2
                     && double.TryParse(values[0], out var widthRatio)
                     && double.TryParse(values[1], out var heightRatio)
                     && widthRatio > 0
                     && heightRatio > 0
            ? widthRatio / heightRatio
            : 16D / 9;
        var width = container.Width;
        var height = (int)Math.Round(width / target);
        if (height > container.Height)
        {
            height = container.Height;
            width = (int)Math.Round(height * target);
        }
        return new Rectangle(container.X + (container.Width - width) / 2, container.Y + (container.Height - height) / 2, width, height);
    }

    private static Rectangle Cover(Rectangle target, Size source)
    {
        var scale = Math.Max((double)target.Width / source.Width, (double)target.Height / source.Height);
        var width = (int)Math.Ceiling(source.Width * scale);
        var height = (int)Math.Ceiling(source.Height * scale);
        return new Rectangle(target.X + (target.Width - width) / 2, target.Y + (target.Height - height) / 2, width, height);
    }

    private static Color ApplyBrightness(Color color, int brightness)
    {
        var factor = 1F + brightness / 100F;
        return Color.FromArgb(color.A, Math.Clamp((int)(color.R * factor), 0, 255), Math.Clamp((int)(color.G * factor), 0, 255), Math.Clamp((int)(color.B * factor), 0, 255));
    }
}
