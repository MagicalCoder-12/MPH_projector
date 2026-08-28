using System.Drawing.Drawing2D;

namespace ChurchProjector;

internal enum RibbonGlyph
{
    Paste,
    Copy,
    Cut,
    AlignLeft,
    AlignCenter,
    AlignRight,
    AlignJustify,
    Bullets,
    Numbered,
    IndentLeft,
    IndentRight,
    ClearFormatting,
    NewDocument,
    Import,
    Delete,
    AddAgenda,
    Screen,
    Projector
}

/// <summary>
/// Flat Word-style ribbon button: a monochrome glyph drawn on top with a small caption below.
/// </summary>
internal sealed class RibbonIconButton : Button
{
    public static readonly Color IdleBack = Color.White;
    public static readonly Color IdleBorder = Color.FromArgb(217, 222, 231);
    public static readonly Color HoverBack = Color.FromArgb(232, 238, 255);
    public static readonly Color ActiveBack = Color.FromArgb(220, 230, 255);
    public static readonly Color ActiveBorder = Color.FromArgb(65, 105, 225);
    public static readonly Color IdleInk = Color.FromArgb(52, 64, 84);
    public static readonly Color ActiveInk = Color.FromArgb(49, 84, 179);
    public static readonly Color DisabledInk = Color.FromArgb(152, 162, 179);
    public static readonly Color DisabledBorder = Color.FromArgb(231, 234, 240);

    private bool _hover;

    public RibbonIconButton()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = IdleBack;
        ForeColor = IdleInk;
        UseVisualStyleBackColor = false;
        Font = new Font("Segoe UI", 8F);
        Margin = new Padding(0, 0, 6, 0);
    }

    public RibbonGlyph Glyph { get; set; }

    /// <summary>Word-style "current setting" highlight (for example the active paragraph alignment).</summary>
    public bool Highlighted { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientSize.Width < 2 || ClientSize.Height < 2) return;
        var g = e.Graphics;
        var body = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        var active = Highlighted;
        var back = active ? ActiveBack : _hover && Enabled ? HoverBack : IdleBack;
        using (var backBrush = new SolidBrush(back))
        {
            g.FillRectangle(backBrush, body);
        }
        using (var borderPen = new Pen(active ? ActiveBorder : Enabled ? IdleBorder : DisabledBorder))
        {
            g.DrawRectangle(borderPen, body);
        }

        var ink = active ? ActiveInk : Enabled ? IdleInk : DisabledInk;
        var captionHeight = string.IsNullOrEmpty(Text) ? 0 : 15;
        var iconArea = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height - captionHeight);
        DrawGlyph(g, new PointF(iconArea.X + (iconArea.Width - 24F) / 2F, iconArea.Y + (iconArea.Height - 24F) / 2F), ink, back);

        if (captionHeight > 0)
        {
            var captionRect = new Rectangle(0, ClientSize.Height - captionHeight, ClientSize.Width, captionHeight);
            TextRenderer.DrawText(g, Text, Font, captionRect, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    private void DrawGlyph(Graphics g, PointF o, Color ink, Color back)
    {
        var x = o.X;
        var y = o.Y;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(ink, 1.6F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var fill = new SolidBrush(ink);
        using var backBrush = new SolidBrush(back);

        switch (Glyph)
        {
            case RibbonGlyph.Paste:
            {
                using var board = RoundedRect(new RectangleF(x + 5F, y + 4.5F, 14F, 16.5F), 2F);
                g.DrawPath(pen, board);
                g.DrawLine(pen, x + 7.5F, y + 10.5F, x + 16.5F, y + 10.5F);
                g.DrawLine(pen, x + 7.5F, y + 13.5F, x + 16.5F, y + 13.5F);
                g.DrawLine(pen, x + 7.5F, y + 16.5F, x + 14F, y + 16.5F);
                using var clip = RoundedRect(new RectangleF(x + 9.5F, y + 2.5F, 5F, 4F), 1.5F);
                g.FillPath(backBrush, clip);
                g.DrawPath(pen, clip);
                break;
            }
            case RibbonGlyph.Copy:
            {
                g.DrawRectangle(pen, new RectangleF(x + 5F, y + 4F, 10.5F, 13.5F));
                g.FillRectangle(backBrush, new RectangleF(x + 9F, y + 8F, 10.5F, 13.5F));
                g.DrawRectangle(pen, new RectangleF(x + 9F, y + 8F, 10.5F, 13.5F));
                g.DrawLine(pen, x + 11.5F, y + 11F, x + 17F, y + 11F);
                g.DrawLine(pen, x + 11.5F, y + 14F, x + 17F, y + 14F);
                g.DrawLine(pen, x + 11.5F, y + 17F, x + 15F, y + 17F);
                break;
            }
            case RibbonGlyph.Cut:
            {
                g.DrawLine(pen, x + 6F, y + 7.4F, x + 19F, y + 16.5F);
                g.DrawLine(pen, x + 6F, y + 16.6F, x + 19F, y + 7.5F);
                g.DrawEllipse(pen, x + 1.9F, y + 4.2F, 4.6F, 4.6F);
                g.DrawEllipse(pen, x + 1.9F, y + 15.2F, 4.6F, 4.6F);
                break;
            }
            case RibbonGlyph.AlignLeft:
            {
                DrawAlignLines(g, pen, x, y, AlignMode.Left);
                break;
            }
            case RibbonGlyph.AlignCenter:
            {
                DrawAlignLines(g, pen, x, y, AlignMode.Center);
                break;
            }
            case RibbonGlyph.AlignRight:
            {
                DrawAlignLines(g, pen, x, y, AlignMode.Right);
                break;
            }
            case RibbonGlyph.AlignJustify:
            {
                DrawAlignLines(g, pen, x, y, AlignMode.Justify);
                break;
            }
            case RibbonGlyph.Bullets:
            {
                g.FillEllipse(fill, x + 4.4F, y + 4.4F, 3F, 3F);
                g.FillEllipse(fill, x + 4.4F, y + 10.4F, 3F, 3F);
                g.FillEllipse(fill, x + 4.4F, y + 16.4F, 3F, 3F);
                g.DrawLine(pen, x + 10F, y + 5.9F, x + 20F, y + 5.9F);
                g.DrawLine(pen, x + 10F, y + 11.9F, x + 20F, y + 11.9F);
                g.DrawLine(pen, x + 10F, y + 17.9F, x + 20F, y + 17.9F);
                break;
            }
            case RibbonGlyph.Numbered:
            {
                using var digitFont = new Font("Segoe UI", 6.5F, FontStyle.Bold);
                g.DrawString("1", digitFont, fill, x + 3.5F, y + 2.5F);
                g.DrawString("2", digitFont, fill, x + 3.5F, y + 8.5F);
                g.DrawString("3", digitFont, fill, x + 3.5F, y + 14.5F);
                g.DrawLine(pen, x + 10F, y + 5.9F, x + 20F, y + 5.9F);
                g.DrawLine(pen, x + 10F, y + 11.9F, x + 20F, y + 11.9F);
                g.DrawLine(pen, x + 10F, y + 17.9F, x + 20F, y + 17.9F);
                break;
            }
            case RibbonGlyph.IndentLeft:
            {
                g.DrawLine(pen, x + 4F, y + 4.5F, x + 20F, y + 4.5F);
                g.DrawLine(pen, x + 10F, y + 9.5F, x + 20F, y + 9.5F);
                g.DrawLine(pen, x + 4F, y + 14.5F, x + 20F, y + 14.5F);
                g.DrawLine(pen, x + 14F, y + 19.5F, x + 8F, y + 19.5F);
                g.DrawLine(pen, x + 10.5F, y + 17F, x + 8F, y + 19.5F);
                g.DrawLine(pen, x + 10.5F, y + 22F, x + 8F, y + 19.5F);
                break;
            }
            case RibbonGlyph.IndentRight:
            {
                g.DrawLine(pen, x + 4F, y + 4.5F, x + 20F, y + 4.5F);
                g.DrawLine(pen, x + 4F, y + 9.5F, x + 14F, y + 9.5F);
                g.DrawLine(pen, x + 4F, y + 14.5F, x + 20F, y + 14.5F);
                g.DrawLine(pen, x + 10F, y + 19.5F, x + 16F, y + 19.5F);
                g.DrawLine(pen, x + 13.5F, y + 17F, x + 16F, y + 19.5F);
                g.DrawLine(pen, x + 13.5F, y + 22F, x + 16F, y + 19.5F);
                break;
            }
            case RibbonGlyph.ClearFormatting:
            {
                using var letterFont = new Font("Segoe UI", 11.5F, FontStyle.Bold);
                using var letterFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("A", letterFont, fill, new RectangleF(x + 1F, y - 1.5F, 22F, 15F), letterFormat);
                var state = g.Save();
                g.TranslateTransform(x + 16.5F, y + 17.5F);
                g.RotateTransform(-18F);
                using var eraserBrush = new SolidBrush(Color.FromArgb(196, 93, 88));
                using var eraser = RoundedRect(new RectangleF(-4F, -2.4F, 8F, 4.8F), 1.6F);
                g.FillPath(eraserBrush, eraser);
                g.Restore(state);
                break;
            }
            case RibbonGlyph.NewDocument:
            {
                using var document = new GraphicsPath();
                document.AddLines(new[]
                {
                    new PointF(x + 5.5F, y + 3.5F),
                    new PointF(x + 14F, y + 3.5F),
                    new PointF(x + 17.5F, y + 7F),
                    new PointF(x + 17.5F, y + 19.5F),
                    new PointF(x + 5.5F, y + 19.5F),
                    new PointF(x + 5.5F, y + 3.5F)
                });
                g.DrawPath(pen, document);
                g.DrawLine(pen, x + 14F, y + 3.5F, x + 14F, y + 7F);
                g.DrawLine(pen, x + 14F, y + 7F, x + 17.5F, y + 7F);
                using var halo = new Pen(back, 4.5F);
                g.DrawLine(halo, x + 13F, y + 17F, x + 21F, y + 17F);
                g.DrawLine(halo, x + 17F, y + 13F, x + 17F, y + 21F);
                g.DrawLine(pen, x + 13F, y + 17F, x + 21F, y + 17F);
                g.DrawLine(pen, x + 17F, y + 13F, x + 17F, y + 21F);
                break;
            }
            case RibbonGlyph.Import:
            {
                using var tray = new GraphicsPath();
                tray.AddLines(new[]
                {
                    new PointF(x + 4F, y + 12F),
                    new PointF(x + 4F, y + 19.5F),
                    new PointF(x + 20F, y + 19.5F),
                    new PointF(x + 20F, y + 12F)
                });
                g.DrawPath(pen, tray);
                g.DrawLine(pen, x + 12F, y + 3F, x + 12F, y + 13.5F);
                g.DrawLine(pen, x + 8.5F, y + 10F, x + 12F, y + 13.5F);
                g.DrawLine(pen, x + 15.5F, y + 10F, x + 12F, y + 13.5F);
                break;
            }
            case RibbonGlyph.Delete:
            {
                g.DrawLine(pen, x + 4F, y + 5.5F, x + 20F, y + 5.5F);
                g.DrawLine(pen, x + 9.5F, y + 5.5F, x + 9.5F, y + 3F);
                g.DrawLine(pen, x + 9.5F, y + 3F, x + 14.5F, y + 3F);
                g.DrawLine(pen, x + 14.5F, y + 3F, x + 14.5F, y + 5.5F);
                g.DrawLine(pen, x + 6F, y + 5.5F, x + 7.3F, y + 20F);
                g.DrawLine(pen, x + 7.3F, y + 20F, x + 16.7F, y + 20F);
                g.DrawLine(pen, x + 16.7F, y + 20F, x + 18F, y + 5.5F);
                g.DrawLine(pen, x + 10.2F, y + 9.5F, x + 10.6F, y + 16.5F);
                g.DrawLine(pen, x + 13.8F, y + 9.5F, x + 13.4F, y + 16.5F);
                break;
            }
            case RibbonGlyph.AddAgenda:
            {
                g.DrawLine(pen, x + 5F, y + 5F, x + 19F, y + 5F);
                g.DrawLine(pen, x + 5F, y + 10F, x + 14F, y + 10F);
                g.DrawLine(pen, x + 11F, y + 16F, x + 19F, y + 16F);
                g.DrawLine(pen, x + 15F, y + 12F, x + 15F, y + 20F);
                break;
            }
            case RibbonGlyph.Screen:
            {
                using var monitor = RoundedRect(new RectangleF(x + 3.5F, y + 4F, 17F, 11.5F), 1.5F);
                g.DrawPath(pen, monitor);
                g.DrawLine(pen, x + 12F, y + 15.5F, x + 12F, y + 18.5F);
                g.DrawLine(pen, x + 8F, y + 19F, x + 16F, y + 19F);
                using var play = new GraphicsPath();
                play.AddPolygon(new[]
                {
                    new PointF(x + 10F, y + 6.8F),
                    new PointF(x + 10F, y + 12.7F),
                    new PointF(x + 14.6F, y + 9.75F)
                });
                g.FillPath(fill, play);
                break;
            }
            case RibbonGlyph.Projector:
            {
                using var body = RoundedRect(new RectangleF(x + 3F, y + 7F, 18F, 10F), 2F);
                g.DrawPath(pen, body);
                g.DrawEllipse(pen, x + 5.6F, y + 9.4F, 4.8F, 4.8F);
                g.FillEllipse(fill, x + 15.6F, y + 9.2F, 2.2F, 2.2F);
                g.DrawLine(pen, x + 15F, y + 14.5F, x + 18.6F, y + 14.5F);
                g.DrawLine(pen, x + 7F, y + 19.5F, x + 6F, y + 21F);
                g.DrawLine(pen, x + 17F, y + 19.5F, x + 18F, y + 21F);
                break;
            }
        }
    }

    private enum AlignMode
    {
        Left,
        Center,
        Right,
        Justify
    }

    private static void DrawAlignLines(Graphics g, Pen pen, float x, float y, AlignMode mode)
    {
        var widths = new[] { 16F, 10F, 16F, 10F };
        for (var i = 0; i < widths.Length; i++)
        {
            var top = y + 4.5F + i * 5F;
            var width = mode == AlignMode.Justify && i < 3 ? 16F : widths[i];
            float start;
            switch (mode)
            {
                case AlignMode.Center:
                    start = x + 12F - width / 2F;
                    break;
                case AlignMode.Right:
                    start = x + 20F - width;
                    break;
                default:
                    start = x + 4F;
                    break;
            }
            g.DrawLine(pen, start, top, start + width, top);
        }
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2F, Math.Min(bounds.Width, bounds.Height));
        if (d <= 0F)
        {
            path.AddRectangle(bounds);
            return path;
        }
        path.AddArc(bounds.X, bounds.Y, d, d, 180F, 90F);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270F, 90F);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0F, 90F);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90F, 90F);
        path.CloseFigure();
        return path;
    }
}

/// <summary>
/// Word-style "A ▬" swatch button used for font colour and highlight colour.
/// </summary>
internal sealed class ColorButton : Button
{
    private bool _hover;
    private readonly Font _iconFont;

    public ColorButton(string icon, Color swatch, Font iconFont)
    {
        _iconFont = iconFont;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = RibbonIconButton.IdleBack;
        ForeColor = RibbonIconButton.IdleInk;
        UseVisualStyleBackColor = false;
        Swatch = swatch;
        Icon = icon;
        Margin = new Padding(0, 0, 6, 0);
    }

    public string Icon { get; set; }
    public Color Swatch { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientSize.Width < 2 || ClientSize.Height < 2) return;
        var g = e.Graphics;
        var body = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        using (var backBrush = new SolidBrush(_hover && Enabled ? RibbonIconButton.HoverBack : RibbonIconButton.IdleBack))
        {
            g.FillRectangle(backBrush, body);
        }
        using (var borderPen = new Pen(Enabled ? RibbonIconButton.IdleBorder : RibbonIconButton.DisabledBorder))
        {
            g.DrawRectangle(borderPen, body);
        }
        var ink = Enabled ? Color.FromArgb(45, 58, 72) : RibbonIconButton.DisabledInk;
        var iconRect = new Rectangle(0, 1, ClientSize.Width, ClientSize.Height - 10);
        TextRenderer.DrawText(g, Icon, _iconFont, iconRect, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        var swatchRect = new Rectangle(3, ClientSize.Height - 6, ClientSize.Width - 7, 4);
        using (var swatchBrush = new SolidBrush(Swatch))
        {
            g.FillRectangle(swatchBrush, swatchRect);
        }
        using (var swatchBorder = new Pen(Color.FromArgb(130, 92, 104, 118)))
        {
            g.DrawRectangle(swatchBorder, swatchRect);
        }
    }
}
