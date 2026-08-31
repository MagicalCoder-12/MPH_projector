using System.Drawing.Drawing2D;

namespace ChurchProjector;

/// <summary>
/// Centralized Fluent-inspired theme for the entire application.
/// All ribbon and workspace controls consume this instead of hard-coding colors.
/// </summary>
internal static class AppTheme
{
    // ── Royal Blue Brand ──────────────────────────────────────────
    public static readonly Color Accent        = Color.FromArgb(65, 105, 225);   // #4169E1
    public static readonly Color AccentDark    = Color.FromArgb(49, 84, 179);    // #3154B3
    public static readonly Color AccentDarker  = Color.FromArgb(37, 66, 143);    // #25428F
    public static readonly Color AccentLight   = Color.FromArgb(232, 238, 255);  // #E8EEFF
    public static readonly Color AccentLighter = Color.FromArgb(243, 246, 255);  // #F3F6FF
    public static readonly Color AccentHover   = Color.FromArgb(53, 94, 208);    // #355ED0
    public static readonly Color AccentPressed = Color.FromArgb(41, 78, 186);    // #294EBA
    public static readonly Color AccentDisabled= Color.FromArgb(170, 188, 235);  // #AABCEB

    // ── Neutrals ──────────────────────────────────────────────────
    public static readonly Color AppBackground   = Color.FromArgb(245, 247, 250); // #F5F7FA
    public static readonly Color Surface         = Color.White;
    public static readonly Color SurfaceSecondary= Color.FromArgb(248, 249, 251); // #F8F9FB
    public static readonly Color SurfaceHover    = Color.FromArgb(240, 243, 248); // #F0F3F8
    public static readonly Color SurfacePressed  = Color.FromArgb(230, 234, 240);
    public static readonly Color Border          = Color.FromArgb(217, 222, 231); // #D9DEE7
    public static readonly Color BorderSubtle    = Color.FromArgb(231, 234, 240); // #E7EAF0
    public static readonly Color BorderFocus     = Accent;

    // ── Text ──────────────────────────────────────────────────────
    public static readonly Color TextPrimary    = Color.FromArgb(31, 41, 55);    // #1F2937
    public static readonly Color TextSecondary  = Color.FromArgb(102, 112, 133); // #667085
    public static readonly Color TextTertiary   = Color.FromArgb(152, 162, 179); // #98A2B3
    public static readonly Color TextDisabled   = Color.FromArgb(152, 162, 179);
    public static readonly Color TextOnAccent   = Color.White;
    public static readonly Color TextInverse    = Color.White;

    // ── Semantic ──────────────────────────────────────────────────
    public static readonly Color Success  = Color.FromArgb(22, 128, 60);   // #16803C
    public static readonly Color Warning  = Color.FromArgb(183, 121, 31);  // #B7791F
    public static readonly Color Danger   = Color.FromArgb(198, 40, 40);   // #C62828
    public static readonly Color Info     = Accent;

    // ── Ribbon ────────────────────────────────────────────────────
    public static readonly Color RibbonBackground  = Surface;
    public static readonly Color RibbonBorder      = BorderSubtle;
    public static readonly Color RibbonGroupSep    = Border;
    public static readonly Color RibbonTabActive   = Accent;
    public static readonly Color RibbonTabInactive = TextSecondary;
    public static readonly Color RibbonTabHover    = AccentLighter;
    public static readonly Color RibbonBtnHover    = AccentLighter;
    public static readonly Color RibbonBtnPressed  = AccentLight;
    public static readonly Color RibbonBtnActiveBg = AccentLight;
    public static readonly Color RibbonBtnActiveBd = Accent;

    // ── Status Bar ────────────────────────────────────────────────
    public static readonly Color StatusBarBg     = AccentDark;
    public static readonly Color StatusBarText   = TextInverse;
    public static readonly Color StatusBarBorder = Accent;

    // ── Header ────────────────────────────────────────────────────
    public static readonly Color HeaderBg       = Accent;
    public static readonly Color HeaderText     = TextInverse;
    public static readonly Color HeaderSubtext  = Color.FromArgb(224, 234, 255);

    // ── Spacing System ────────────────────────────────────────────
    public const int SpaceXXS = 2;
    public const int SpaceXS  = 4;
    public const int SpaceS   = 6;
    public const int SpaceM   = 8;
    public const int SpaceL   = 12;
    public const int SpaceXL  = 16;
    public const int SpaceXXL = 24;

    // ── Ribbon Dimensions ─────────────────────────────────────────
    public const int TabHeight         = 30;
    public const int RibbonMinHeight   = 110;
    public const int GroupHeaderHeight = 18;
    public const int LargeIconSize     = 32;
    public const int SmallIconSize     = 16;
    public const int ButtonHeight      = 26;
    public const int LargeButtonHeight = 62;
    public const int ComboBoxWidth     = 140;
    public const int SeparatorWidth    = 1;
    public const int HeaderHeight      = 40;

    // ── Drawing Helpers ───────────────────────────────────────────

    /// <summary>Rounded rectangle path for modern controls.</summary>
    public static GraphicsPath RoundedRect(Rectangle bounds, float radius)
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

    /// <summary>Draw a small rounded highlight pill (used for toggle indicators).</summary>
    public static void DrawIndicator(Graphics g, Rectangle bounds, Color color, int radius = 2)
    {
        using var brush = new SolidBrush(color);
        using var path = RoundedRect(bounds, radius);
        g.FillPath(brush, path);
    }

    /// <summary>Draw text with proper antialiasing.</summary>
    public static void DrawText(Graphics g, string text, Font font, Color color, Rectangle bounds,
        TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter)
    {
        TextRenderer.DrawText(g, text, font, bounds, color, flags | TextFormatFlags.NoPrefix);
    }

    /// <summary>Measure text width for layout calculations.</summary>
    public static int MeasureText(string text, Font font)
    {
        return TextRenderer.MeasureText(text, font).Width;
    }

    /// <summary>Create a scaled font (handles DPI automatically).</summary>
    public static Font GetFont(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("Segoe UI", size, style, GraphicsUnit.Point);
    }

    /// <summary>Solid brush with given color.</summary>
    public static SolidBrush Brush(Color c) => new(c);

    /// <summary>Pen with given color and width.</summary>
    public static Pen Pen(Color c, float width = 1F) => new(c, width) { Alignment = PenAlignment.Inset };
}
