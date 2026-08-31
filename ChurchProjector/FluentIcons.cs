using System.Drawing.Drawing2D;

namespace ChurchProjector;

/// <summary>
/// Consistent Fluent-inspired icon drawing system.
/// All icons are drawn programmatically with consistent visual weight, stroke width, and style.
/// No emoji, no mixed icon families — one coherent icon language.
/// </summary>
internal static class FluentIcons
{
    private static Pen MakePen(Color c, float w = 1.4F) => new(c, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

    // ── Clipboard ─────────────────────────────────────────────

    public static void Paste(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        // Board
        var board = new RectangleF(b.X + 5, b.Y + 3, 14, 16);
        using (var path = RoundedRectF(board, 2)) g.DrawPath(pen, path);
        // Clip
        using (var clip = RoundedRectF(new RectangleF(b.X + 9, b.Y + 1, 6, 4), 1.5F))
        {
            g.FillPath(AppTheme.Brush(c), clip);
            g.DrawPath(pen, clip);
        }
        // Lines
        g.DrawLine(pen, b.X + 8, b.Y + 10, b.X + 16, b.Y + 10);
        g.DrawLine(pen, b.X + 8, b.Y + 13, b.X + 16, b.Y + 13);
        g.DrawLine(pen, b.X + 8, b.Y + 16, b.X + 14, b.Y + 16);
    }

    public static void Copy(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        g.DrawRectangle(pen, b.X + 4, b.Y + 3, 11, 14);
        g.DrawRectangle(pen, b.X + 8, b.Y + 7, 11, 14);
        g.DrawLine(pen, b.X + 11, b.Y + 11, b.X + 17, b.Y + 11);
        g.DrawLine(pen, b.X + 11, b.Y + 14, b.X + 17, b.Y + 14);
        g.DrawLine(pen, b.X + 11, b.Y + 17, b.X + 15, b.Y + 17);
    }

    public static void Cut(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        g.DrawLine(pen, b.X + 5, b.Y + 7, b.X + 19, b.Y + 17);
        g.DrawLine(pen, b.X + 5, b.Y + 17, b.X + 19, b.Y + 7);
        g.DrawEllipse(pen, b.X + 2, b.Y + 4, 5, 5);
        g.DrawEllipse(pen, b.X + 2, b.Y + 15, 5, 5);
    }

    // ── Alignment ─────────────────────────────────────────────

    public static void AlignLeft(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c, 1.6F);
        g.DrawLine(pen, b.X + 4, b.Y + 4, b.X + 20, b.Y + 4);
        g.DrawLine(pen, b.X + 4, b.Y + 9, b.X + 14, b.Y + 9);
        g.DrawLine(pen, b.X + 4, b.Y + 14, b.X + 20, b.Y + 14);
        g.DrawLine(pen, b.X + 4, b.Y + 19, b.X + 16, b.Y + 19);
    }

    public static void AlignCenter(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c, 1.6F);
        g.DrawLine(pen, b.X + 4, b.Y + 4, b.X + 20, b.Y + 4);
        g.DrawLine(pen, b.X + 7, b.Y + 9, b.X + 17, b.Y + 9);
        g.DrawLine(pen, b.X + 4, b.Y + 14, b.X + 20, b.Y + 14);
        g.DrawLine(pen, b.X + 6, b.Y + 19, b.X + 18, b.Y + 19);
    }

    public static void AlignRight(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c, 1.6F);
        g.DrawLine(pen, b.X + 4, b.Y + 4, b.X + 20, b.Y + 4);
        g.DrawLine(pen, b.X + 10, b.Y + 9, b.X + 20, b.Y + 9);
        g.DrawLine(pen, b.X + 4, b.Y + 14, b.X + 20, b.Y + 14);
        g.DrawLine(pen, b.X + 8, b.Y + 19, b.X + 20, b.Y + 19);
    }

    public static void AlignJustify(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c, 1.6F);
        g.DrawLine(pen, b.X + 4, b.Y + 4, b.X + 20, b.Y + 4);
        g.DrawLine(pen, b.X + 4, b.Y + 9, b.X + 20, b.Y + 9);
        g.DrawLine(pen, b.X + 4, b.Y + 14, b.X + 20, b.Y + 14);
        g.DrawLine(pen, b.X + 4, b.Y + 19, b.X + 20, b.Y + 19);
    }

    // ── Lists & Indent ────────────────────────────────────────

    public static void Bullets(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        using var fill = AppTheme.Brush(c);
        g.FillEllipse(fill, b.X + 4, b.Y + 4, 3, 3);
        g.FillEllipse(fill, b.X + 4, b.Y + 10, 3, 3);
        g.FillEllipse(fill, b.X + 4, b.Y + 16, 3, 3);
        g.DrawLine(pen, b.X + 10, b.Y + 5.5F, b.X + 20, b.Y + 5.5F);
        g.DrawLine(pen, b.X + 10, b.Y + 11.5F, b.X + 20, b.Y + 11.5F);
        g.DrawLine(pen, b.X + 10, b.Y + 17.5F, b.X + 20, b.Y + 17.5F);
    }

    public static void Numbering(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        using var fill = AppTheme.Brush(c);
        using var font = AppTheme.GetFont(6.5F, FontStyle.Bold);
        g.DrawString("1", font, fill, b.X + 3, b.Y + 2);
        g.DrawString("2", font, fill, b.X + 3, b.Y + 8);
        g.DrawString("3", font, fill, b.X + 3, b.Y + 14);
        g.DrawLine(pen, b.X + 10, b.Y + 5.5F, b.X + 20, b.Y + 5.5F);
        g.DrawLine(pen, b.X + 10, b.Y + 11.5F, b.X + 20, b.Y + 11.5F);
        g.DrawLine(pen, b.X + 10, b.Y + 17.5F, b.X + 20, b.Y + 17.5F);
    }

    public static void IndentLeft(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        g.DrawLine(pen, b.X + 4, b.Y + 4, b.X + 20, b.Y + 4);
        g.DrawLine(pen, b.X + 10, b.Y + 9, b.X + 20, b.Y + 9);
        g.DrawLine(pen, b.X + 4, b.Y + 14, b.X + 20, b.Y + 14);
        // Arrow
        g.DrawLine(pen, b.X + 14, b.Y + 19, b.X + 8, b.Y + 19);
        g.DrawLine(pen, b.X + 10, b.Y + 17, b.X + 8, b.Y + 19);
        g.DrawLine(pen, b.X + 10, b.Y + 21, b.X + 8, b.Y + 19);
    }

    public static void IndentRight(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        g.DrawLine(pen, b.X + 4, b.Y + 4, b.X + 20, b.Y + 4);
        g.DrawLine(pen, b.X + 4, b.Y + 9, b.X + 14, b.Y + 9);
        g.DrawLine(pen, b.X + 4, b.Y + 14, b.X + 20, b.Y + 14);
        // Arrow
        g.DrawLine(pen, b.X + 10, b.Y + 19, b.X + 16, b.Y + 19);
        g.DrawLine(pen, b.X + 13.5F, b.Y + 17, b.X + 16, b.Y + 19);
        g.DrawLine(pen, b.X + 13.5F, b.Y + 21, b.X + 16, b.Y + 19);
    }

    // ── Clear Formatting ──────────────────────────────────────

    public static void ClearFormatting(Graphics g, Rectangle b, Color c)
    {
        using var fill = AppTheme.Brush(c);
        using var font = AppTheme.GetFont(11F, FontStyle.Bold);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("A", font, fill, new RectangleF(b.X + 1, b.Y - 1, 22, 14), format);
        // Eraser slash
        var state = g.Save();
        g.TranslateTransform(b.X + 16, b.Y + 17);
        g.RotateTransform(-18);
        using var eraser = AppTheme.Brush(AppTheme.Danger);
        using var path = RoundedRectF(new RectangleF(-4, -2.4F, 8, 4.8F), 1.6F);
        g.FillPath(eraser, path);
        g.Restore(state);
    }

    // ── Document ──────────────────────────────────────────────

    public static void NewDocument(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        // Page outline
        g.DrawLines(pen, [new PointF(b.X + 5, b.Y + 3), new PointF(b.X + 14, b.Y + 3),
            new PointF(b.X + 17, b.Y + 7), new PointF(b.X + 17, b.Y + 20),
            new PointF(b.X + 5, b.Y + 20), new PointF(b.X + 5, b.Y + 3)]);
        // Corner fold
        g.DrawLine(pen, b.X + 14, b.Y + 3, b.X + 14, b.Y + 7);
        g.DrawLine(pen, b.X + 14, b.Y + 7, b.X + 17, b.Y + 7);
        // Plus
        using var halo = new Pen(AppTheme.Surface, 4F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(halo, b.X + 13, b.Y + 17, b.X + 21, b.Y + 17);
        g.DrawLine(halo, b.X + 17, b.Y + 13, b.X + 17, b.Y + 21);
        g.DrawLine(pen, b.X + 13, b.Y + 17, b.X + 21, b.Y + 17);
        g.DrawLine(pen, b.X + 17, b.Y + 13, b.X + 17, b.Y + 21);
    }

    public static void Import(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        // Tray
        g.DrawLines(pen, [new PointF(b.X + 4, b.Y + 12), new PointF(b.X + 4, b.Y + 20),
            new PointF(b.X + 20, b.Y + 20), new PointF(b.X + 20, b.Y + 12)]);
        // Arrow
        g.DrawLine(pen, b.X + 12, b.Y + 3, b.X + 12, b.Y + 14);
        g.DrawLine(pen, b.X + 9, b.Y + 10, b.X + 12, b.Y + 14);
        g.DrawLine(pen, b.X + 15, b.Y + 10, b.X + 12, b.Y + 14);
    }

    public static void Delete(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        // Trash can
        g.DrawLine(pen, b.X + 5, b.Y + 5, b.X + 19, b.Y + 5);
        g.DrawLine(pen, b.X + 9, b.Y + 5, b.X + 9, b.Y + 3);
        g.DrawLine(pen, b.X + 9, b.Y + 3, b.X + 15, b.Y + 3);
        g.DrawLine(pen, b.X + 15, b.Y + 3, b.X + 15, b.Y + 5);
        g.DrawLines(pen, [new PointF(b.X + 6, b.Y + 5), new PointF(b.X + 7, b.Y + 20),
            new PointF(b.X + 17, b.Y + 20), new PointF(b.X + 18, b.Y + 5)]);
        g.DrawLine(pen, b.X + 10, b.Y + 9, b.X + 10.5F, b.Y + 17);
        g.DrawLine(pen, b.X + 14, b.Y + 9, b.X + 13.5F, b.Y + 17);
    }

    // ── Agenda ────────────────────────────────────────────────

    public static void AddAgenda(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        g.DrawLine(pen, b.X + 5, b.Y + 5, b.X + 19, b.Y + 5);
        g.DrawLine(pen, b.X + 5, b.Y + 10, b.X + 14, b.Y + 10);
        g.DrawLine(pen, b.X + 11, b.Y + 16, b.X + 19, b.Y + 16);
        g.DrawLine(pen, b.X + 15, b.Y + 12, b.X + 15, b.Y + 20);
    }

    // ── Presentation ──────────────────────────────────────────

    public static void Screen(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        using var fill = AppTheme.Brush(c);
        // Monitor
        using (var path = RoundedRectF(new RectangleF(b.X + 3, b.Y + 3, 18, 12), 1.5F))
            g.DrawPath(pen, path);
        // Stand
        g.DrawLine(pen, b.X + 12, b.Y + 15, b.X + 12, b.Y + 18);
        g.DrawLine(pen, b.X + 8, b.Y + 19, b.X + 16, b.Y + 19);
        // Play triangle
        var play = new PointF[] { new(b.X + 10, b.Y + 6), new(b.X + 10, b.Y + 13), new(b.X + 14, b.Y + 9.5F) };
        g.FillPolygon(fill, play);
    }

    public static void Projector(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        using var fill = AppTheme.Brush(c);
        // Body
        using (var path = RoundedRectF(new RectangleF(b.X + 3, b.Y + 6, 18, 10), 2))
            g.DrawPath(pen, path);
        // Lens
        g.DrawEllipse(pen, b.X + 6, b.Y + 9, 5, 5);
        g.FillEllipse(fill, b.X + 16, b.Y + 9, 2, 2);
        // Light beam
        g.DrawLine(pen, b.X + 15, b.Y + 14, b.X + 19, b.Y + 14);
        // Legs
        g.DrawLine(pen, b.X + 7, b.Y + 19, b.X + 6, b.Y + 21);
        g.DrawLine(pen, b.X + 17, b.Y + 19, b.X + 18, b.Y + 21);
    }

    // ── Cloud ─────────────────────────────────────────────────

    public static void CloudSync(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        using var fill = AppTheme.Brush(AppTheme.Surface);
        // Cloud shape
        var cloud = new GraphicsPath();
        cloud.AddArc(b.X + 5, b.Y + 8, 12, 10, 180, 180);
        cloud.AddArc(b.X + 10, b.Y + 3, 10, 10, 180, 180);
        cloud.AddArc(b.X + 3, b.Y + 6, 8, 8, 180, 180);
        cloud.CloseFigure();
        g.FillPath(fill, cloud);
        g.DrawPath(pen, cloud);
        // Arrows
        g.DrawLine(pen, b.X + 12, b.Y + 13, b.X + 12, b.Y + 19);
        g.DrawLine(pen, b.X + 9, b.Y + 16, b.X + 12, b.Y + 19);
        g.DrawLine(pen, b.X + 15, b.Y + 16, b.X + 12, b.Y + 19);
    }

    // ── Sun/Brightness ────────────────────────────────────────

    public static void Sun(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        // Circle
        g.DrawEllipse(pen, b.X + 7, b.Y + 7, 10, 10);
        // Rays
        for (var i = 0; i < 8; i++)
        {
            var angle = i * 45F * MathF.PI / 180F;
            var cx = b.X + 12; var cy = b.Y + 12;
            var r1 = 7F; var r2 = 9.5F;
            g.DrawLine(pen, cx + MathF.Cos(angle) * r1, cy + MathF.Sin(angle) * r1,
                cx + MathF.Cos(angle) * r2, cy + MathF.Sin(angle) * r2);
        }
    }

    // ── No animation ──────────────────────────────────────────

    public static void NoAnimation(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        g.DrawRectangle(pen, b.X + 6, b.Y + 6, 12, 12);
    }

    public static void CrossFade(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        g.DrawRectangle(pen, b.X + 4, b.Y + 6, 8, 12);
        g.DrawRectangle(pen, b.X + 12, b.Y + 6, 8, 12);
        // Overlap indication
        using var fill = AppTheme.Brush(Color.FromArgb(40, c));
        g.FillRectangle(fill, b.X + 10, b.Y + 6, 4, 12);
    }

    public static void ZoomIn(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        g.DrawLine(pen, b.X + 6, b.Y + 6, b.X + 16, b.Y + 16);
        g.DrawLine(pen, b.X + 16, b.Y + 6, b.X + 6, b.Y + 16);
        g.DrawLine(pen, b.X + 11, b.Y + 3, b.X + 11, b.Y + 9);
        g.DrawLine(pen, b.X + 8, b.Y + 6, b.X + 14, b.Y + 6);
    }

    public static void ZoomOut(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c);
        g.DrawLine(pen, b.X + 6, b.Y + 6, b.X + 16, b.Y + 16);
        g.DrawLine(pen, b.X + 16, b.Y + 6, b.X + 6, b.Y + 16);
        g.DrawLine(pen, b.X + 8, b.Y + 6, b.X + 14, b.Y + 6);
    }

    public static void AutoFit(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c, 1.5F);
        var inset = Rectangle.Inflate(b, -4, -4);
        g.DrawRectangle(pen, inset);
        g.DrawLine(pen, b.X + 4, b.Y + 8, b.X + 9, b.Y + 8);
        g.DrawLine(pen, b.X + 4, b.Y + 8, b.X + 7, b.Y + 5);
        g.DrawLine(pen, b.X + 4, b.Y + 8, b.X + 7, b.Y + 11);
        g.DrawLine(pen, b.Right - 4, b.Bottom - 8, b.Right - 9, b.Bottom - 8);
        g.DrawLine(pen, b.Right - 4, b.Bottom - 8, b.Right - 7, b.Bottom - 11);
        g.DrawLine(pen, b.Right - 4, b.Bottom - 8, b.Right - 7, b.Bottom - 5);
    }

    // ── Play/Go live ──────────────────────────────────────────

    public static void Play(Graphics g, Rectangle b, Color c)
    {
        using var fill = AppTheme.Brush(c);
        var points = new PointF[] { new(b.X + 6, b.Y + 4), new(b.X + 6, b.Y + 20), new(b.X + 18, b.Y + 12) };
        g.FillPolygon(fill, points);
    }

    public static void Stop(Graphics g, Rectangle b, Color c)
    {
        using var fill = AppTheme.Brush(c);
        g.FillRectangle(fill, b.X + 6, b.Y + 6, 12, 12);
    }

    // ── Arrow navigation ──────────────────────────────────────

    public static void ChevronLeft(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c, 2F);
        g.DrawLine(pen, b.X + 14, b.Y + 6, b.X + 8, b.Y + 12);
        g.DrawLine(pen, b.X + 8, b.Y + 12, b.X + 14, b.Y + 18);
    }

    public static void ChevronRight(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c, 2F);
        g.DrawLine(pen, b.X + 8, b.Y + 6, b.X + 14, b.Y + 12);
        g.DrawLine(pen, b.X + 14, b.Y + 12, b.X + 8, b.Y + 18);
    }

    // ── Sync arrows ───────────────────────────────────────────

    public static void SyncArrows(Graphics g, Rectangle b, Color c)
    {
        using var pen = MakePen(c, 1.6F);
        // Two curved arrows
        g.DrawArc(pen, b.X + 4, b.Y + 3, 14, 14, 180, 120);
        g.DrawArc(pen, b.X + 4, b.Y + 5, 14, 14, 0, 120);
        // Arrowheads
        g.DrawLine(pen, b.X + 6, b.Y + 4, b.X + 4, b.Y + 8);
        g.DrawLine(pen, b.X + 18, b.Y + 16, b.X + 20, b.Y + 12);
    }

    // ── Helpers ───────────────────────────────────────────────

    private static GraphicsPath RoundedRectF(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2F, Math.Min(bounds.Width, bounds.Height));
        if (d <= 0F) { path.AddRectangle(bounds); return path; }
        path.AddArc(bounds.X, bounds.Y, d, d, 180F, 90F);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270F, 90F);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0F, 90F);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90F, 90F);
        path.CloseFigure();
        return path;
    }
}
