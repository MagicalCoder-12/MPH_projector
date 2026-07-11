using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace ChurchProjector;

public sealed class PresentationTheme
{
    public string FontFamily { get; set; } = "Segoe UI";
    public float FontSize { get; set; } = 56;
    public bool Bold { get; set; } = true;
    public Color TextColor { get; set; } = Color.White;
    public string Alignment { get; set; } = "Centre";
    public Color BackgroundColor { get; set; } = Color.FromArgb(22, 34, 52);
    public Image? BackgroundImage { get; set; }
    public Guid? BackgroundAssetId { get; set; }
    public string? BackgroundVideoPath { get; set; }
    public bool VideoLoop { get; set; } = true;
    public int Brightness { get; set; }
    public string AspectRatio { get; set; } = "16:9";
}

public sealed class SlideCanvas : Control
{
    public PresentationTheme? Theme { get; set; }
    public string SlideText { get; set; } = "";

    public SlideCanvas()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
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
        DrawSlide(e.Graphics, canvas, SlideText, theme);
    }

    public static void DrawSlide(Graphics graphics, Rectangle canvas, string text, PresentationTheme theme)
    {
        DrawBackground(graphics, canvas, theme);
        using var shade = new SolidBrush(Color.FromArgb(35, Color.Black));
        graphics.FillRectangle(shade, canvas);

        var style = theme.Bold ? FontStyle.Bold : FontStyle.Regular;
        var availableWidth = Math.Max(40, canvas.Width - (canvas.Width * 16 / 100));
        var fontSize = Math.Max(12, canvas.Width * theme.FontSize / 1280F);
        using var font = new Font(theme.FontFamily, fontSize, style, GraphicsUnit.Pixel);
        using var format = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = theme.Alignment switch { "Left" => StringAlignment.Near, "Right" => StringAlignment.Far, _ => StringAlignment.Center }, Trimming = StringTrimming.Word };
        var textArea = new RectangleF(canvas.X + canvas.Width * .08F, canvas.Y + canvas.Height * .12F, availableWidth, canvas.Height * .76F);
        using var shadow = new SolidBrush(Color.FromArgb(190, Color.Black));
        var shadowArea = new RectangleF(textArea.X + 3, textArea.Y + 4, textArea.Width, textArea.Height);
        graphics.DrawString(text, font, shadow, shadowArea, format);
        using var brush = new SolidBrush(theme.TextColor);
        graphics.DrawString(text, font, brush, textArea, format);
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
