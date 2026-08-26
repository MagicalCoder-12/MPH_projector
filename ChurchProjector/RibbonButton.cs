using System.Drawing.Drawing2D;

namespace ChurchProjector;

/// <summary>
/// A compact ribbon button that draws a large symbol with a small caption
/// underneath, the way production Office-style ribbons do.
/// </summary>
public sealed class RibbonButton : Button
{
    private static readonly Color DefaultBack = Color.FromArgb(232, 237, 244);
    private static readonly Color DefaultFore = Color.FromArgb(31, 48, 68);
    private static readonly Color Brand = Color.FromArgb(22, 113, 180);
    private static readonly Color Border = Color.FromArgb(196, 206, 216);

    private readonly string _symbol;
    private readonly string _caption;
    /// <summary>When set, a colour strip is drawn at the bottom of the button.</summary>
    public Color? Swatch { get; set; }

    public RibbonButton(string symbol, string caption)
    {
        _symbol = symbol;
        _caption = caption;
        Text = string.Empty;
        Width = 56;
        Height = 54;
        Margin = new Padding(0, 0, 4, 0);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = DefaultBack;
        ForeColor = DefaultFore;
        Cursor = Cursors.Hand;
        UseVisualStyleBackColor = false;
        TabStop = true;
    }

    public void SetActive(bool active)
    {
        BackColor = active ? Brand : DefaultBack;
        ForeColor = active ? Color.White : DefaultFore;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        using var background = new SolidBrush(BackColor);
        g.FillRectangle(background, ClientRectangle);
        using var borderPen = new Pen(Border);
        g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        using var symbolFont = new Font("Segoe UI", 11F, FontStyle.Bold);
        using var captionFont = new Font("Segoe UI", 7.2F);
        using var brush = new SolidBrush(ForeColor);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };

        var symbolRect = new RectangleF(0, 3, Width, 22);
        var captionRect = new RectangleF(2, 24, Width - 4, 16);
        g.DrawString(_symbol, symbolFont, brush, symbolRect, format);
        g.DrawString(_caption, captionFont, brush, captionRect, format);

        if (Swatch is { } colour)
        {
            using var swatch = new SolidBrush(colour);
            g.FillRectangle(swatch, 6, Height - 7, Width - 12, 4);
        }
    }
}
