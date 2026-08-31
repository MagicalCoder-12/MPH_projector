using System.Drawing.Drawing2D;

namespace ChurchProjector;

// ═══════════════════════════════════════════════════════════════════════
//  RibbonBar — the top-level ribbon container that manages tabs + content
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// A modern Office-style ribbon bar with tabs, group panels, and content areas.
/// Replaces the old FlowLayoutPanel-based ribbon system.
/// </summary>
internal sealed class RibbonBar : Control
{
    private readonly List<RibbonTabButton> _tabs = [];
    private readonly Panel _contentHost;
    private readonly Panel _tabStrip;
    private RibbonTabButton? _activeTab;
    private readonly Dictionary<string, Control> _tabPages = new(StringComparer.OrdinalIgnoreCase);

    public RibbonBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = AppTheme.RibbonBackground;
        Height = 130;
        Dock = DockStyle.Top;

        // Tab strip at top
        _tabStrip = new Panel
        {
            Dock = DockStyle.Top,
            Height = AppTheme.TabHeight,
            BackColor = AppTheme.RibbonBackground,
            Padding = new Padding(AppTheme.SpaceXL, 0, 0, 0)
        };
        _tabStrip.Paint += TabStrip_Paint;

        // Content host below tabs
        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.RibbonBackground
        };
        _contentHost.Paint += ContentHost_Paint;

        Controls.Add(_contentHost);
        Controls.Add(_tabStrip);
    }

    public event EventHandler<string>? TabChanged;

    /// <summary>Add a tab and return the content panel for that tab.</summary>
    public Control AddTab(string text, bool active = false)
    {
        var tab = new RibbonTabButton(text)
        {
            Height = AppTheme.TabHeight,
            Dock = DockStyle.Left,
            Active = active
        };
        tab.Click += (_, _) => SelectTab(tab);
        _tabs.Add(tab);
        _tabStrip.Controls.Add(tab);

        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.RibbonBackground,
            Visible = active,
            Padding = new Padding(AppTheme.SpaceL, AppTheme.SpaceS, AppTheme.SpaceL, AppTheme.SpaceM)
        };
        _contentHost.Controls.Add(page);
        _tabPages[text] = page;

        if (active) _activeTab = tab;
        return page;
    }

    public void SelectTab(string text)
    {
        if (_tabPages.TryGetValue(text, out var page) && _tabs.FirstOrDefault(t => t.Text == text) is { } tab)
            SelectTab(tab);
    }

    private void SelectTab(RibbonTabButton tab)
    {
        if (_activeTab == tab) return;
        if (_activeTab is not null)
        {
            _activeTab.Active = false;
            if (_tabPages.TryGetValue(_activeTab.Text, out var oldPage)) oldPage.Visible = false;
        }
        tab.Active = true;
        _activeTab = tab;
        if (_tabPages.TryGetValue(tab.Text, out var newPage)) newPage.Visible = true;
        TabChanged?.Invoke(this, tab.Text);
        _tabStrip.Invalidate();
    }

    private void TabStrip_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        // Bottom border line - thicker for visual weight
        using var pen = new Pen(AppTheme.Border);
        g.DrawLine(pen, 0, _tabStrip.Height - 1, _tabStrip.Width, _tabStrip.Height - 1);
    }

    private void ContentHost_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        // Bottom border - distinct from workspace
        using var pen = new Pen(AppTheme.Border);
        g.DrawLine(pen, 0, _contentHost.Height - 1, _contentHost.Width, _contentHost.Height - 1);
    }


}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonTabButton — modern Fluent tab with underline indicator
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonTabButton : Control
{
    private bool _hover;
    private bool _active;
    private Font? _normalFont;
    private Font? _boldFont;

    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value) return;
            _active = value;
            Font = value ? BoldFont : NormalFont;
            Invalidate();
        }
    }

    private Font NormalFont => _normalFont ??= AppTheme.GetFont(9F);
    private Font BoldFont => _boldFont ??= AppTheme.GetFont(9F, FontStyle.Bold);

    public RibbonTabButton(string text)
    {
        Text = text;
        Width = Math.Max(60, AppTheme.MeasureText(text, BoldFont) + AppTheme.SpaceXL * 2);
        Height = AppTheme.TabHeight;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Font = NormalFont;
        ForeColor = AppTheme.RibbonTabInactive;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = ClientRectangle;

        // Background on hover
        if (_hover && !_active)
        {
            using var hoverBrush = AppTheme.Brush(AppTheme.AccentLighter);
            g.FillRectangle(hoverBrush, rect);
        }

        // Text
        var textColor = _active ? AppTheme.Accent : _hover ? AppTheme.TextPrimary : AppTheme.TextSecondary;
        var textRect = new Rectangle(0, 0, Width, Height - 3);
        TextRenderer.DrawText(g, Text, Font, textRect, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        // Underline indicator (2px rounded bar)
        if (_active)
        {
            var indicatorRect = new Rectangle(Width / 2 - 12, Height - 3, 24, 2);
            using var brush = AppTheme.Brush(AppTheme.Accent);
            using var path = AppTheme.RoundedRect(indicatorRect, 1);
            g.FillPath(brush, path);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonGroupPanel — groups related controls with a bottom label
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonGroupPanel : Control
{
    private readonly string _label;
    private readonly FlowLayoutPanel _items;
    private int _preferredWidth;

    public RibbonGroupPanel(string label, int preferredWidth = 0)
    {
        _label = label;
        _preferredWidth = preferredWidth;
        _items = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoSize = false,
            BackColor = AppTheme.RibbonBackground,
            Padding = new Padding(AppTheme.SpaceM, AppTheme.SpaceXS, AppTheme.SpaceM, 0)
        };
        Controls.Add(_items);

        Height = AppTheme.LargeButtonHeight + AppTheme.GroupHeaderHeight + AppTheme.SpaceL;
        if (preferredWidth > 0) Width = preferredWidth;
    }

    public FlowLayoutPanel Items => _items;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Right separator line (between groups)
        if (Parent is FlowLayoutPanel flow && flow.Controls.IndexOf(this) < flow.Controls.Count - 1)
        {
            using var pen = AppTheme.Pen(AppTheme.Border, 1F);
            g.DrawLine(pen, Width - 1, AppTheme.SpaceM, Width - 1, Height - AppTheme.GroupHeaderHeight - AppTheme.SpaceM);
        }

        // Group label at bottom
        var labelRect = new Rectangle(AppTheme.SpaceM, Height - GroupHeaderHeightWithPad, Width - AppTheme.SpaceM, AppTheme.GroupHeaderHeight);
        TextRenderer.DrawText(g, _label, AppTheme.GetFont(7.5F), labelRect,
            AppTheme.TextTertiary,
            TextFormatFlags.Right | TextFormatFlags.Bottom | TextFormatFlags.NoPrefix);
    }

    private int GroupHeaderHeightWithPad => AppTheme.GroupHeaderHeight + AppTheme.SpaceS;

    public void Add(Control control) => _items.Controls.Add(control);
    public void AddRange(params Control[] controls) => _items.Controls.AddRange(controls);
}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonSmallButton — compact button for ribbon groups (icon + caption)
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonSmallButton : Button
{
    private readonly bool _nativeRendering = true;
    private bool _hover;
    private bool _pressed;
    private readonly IconDrawer? _iconDrawer;
    private readonly string _caption;
    private readonly bool _isAccent;
    private readonly Color _accentBg;
    private readonly Color _accentFg;

    public RibbonSmallButton(string caption, IconDrawer? iconDrawer = null, bool accent = false,
        Color accentBg = default, Color accentFg = default, int width = 58, int height = AppTheme.LargeButtonHeight)
    {
        _caption = caption;
        Text = caption;
        _iconDrawer = iconDrawer;
        _isAccent = accent;
        _accentBg = accentBg == default ? AppTheme.Accent : accentBg;
        _accentFg = accentFg == default ? AppTheme.TextOnAccent : accentFg;
        Width = width;
        Height = height;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_nativeRendering) { base.OnPaint(e); return; }
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        // Background
        Color bgColor;
        if (_isAccent)
            bgColor = _pressed ? AppTheme.AccentPressed : _hover ? AppTheme.AccentHover : _accentBg;
        else
            bgColor = _pressed ? AppTheme.SurfacePressed : _hover ? AppTheme.AccentLighter : Color.Transparent;

        if (bgColor != Color.Transparent)
        {
            using var brush = AppTheme.Brush(bgColor);
            using var path = AppTheme.RoundedRect(rect, 4);
            g.FillPath(brush, path);
        }

        // Border (only for non-accent buttons on hover)
        if (!_isAccent && (_hover || _pressed))
        {
            using var pen = AppTheme.Pen(AppTheme.Border);
            using var path = AppTheme.RoundedRect(rect, 4);
            g.DrawPath(pen, path);
        }

        // Icon
        var iconRect = new Rectangle((Width - AppTheme.LargeIconSize) / 2, AppTheme.SpaceS, AppTheme.LargeIconSize, AppTheme.LargeIconSize);
        if (_iconDrawer is not null)
        {
            var iconColor = _isAccent ? _accentFg : _hover ? AppTheme.Accent : AppTheme.TextPrimary;
            _iconDrawer(g, iconRect, iconColor);
        }

        // Caption
        var textRect = new Rectangle(0, iconRect.Bottom + AppTheme.SpaceXXS, Width, AppTheme.GroupHeaderHeight);
        var textColor = _isAccent ? _accentFg : _hover ? AppTheme.TextPrimary : AppTheme.TextSecondary;
        TextRenderer.DrawText(g, _caption, AppTheme.GetFont(8F), textRect, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPrefix);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonTinyButton — compact inline button (28x28, icon only or tiny text)
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonTinyButton : Button
{
    private readonly bool _nativeRendering = true;
    private bool _hover;
    private bool _pressed;
    private bool _active;
    private readonly IconDrawer? _iconDrawer;
    private readonly string? _glyph;
    private readonly Font? _glyphFont;

    public bool Active
    {
        get => _active;
        set { if (_active != value) { _active = value; Invalidate(); } }
    }

    public RibbonTinyButton(string? glyph = null, IconDrawer? iconDrawer = null, int size = 28, string? tooltip = null)
    {
        _glyph = glyph;
        Text = glyph ?? string.Empty;
        _iconDrawer = iconDrawer;
        _glyphFont = glyph is not null ? AppTheme.GetFont(10F) : null;
        Width = size;
        Height = size;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        if (tooltip is not null)
        {
            var tt = new ToolTip();
            tt.SetToolTip(this, tooltip);
        }
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_nativeRendering) { base.OnPaint(e); return; }
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        // Background
        Color bgColor = _active ? AppTheme.RibbonBtnActiveBg
            : _pressed ? AppTheme.SurfacePressed
            : _hover ? AppTheme.AccentLighter
            : Color.Transparent;

        if (bgColor != Color.Transparent)
        {
            using var brush = AppTheme.Brush(bgColor);
            using var path = AppTheme.RoundedRect(rect, 3);
            g.FillPath(brush, path);
        }

        // Active border
        if (_active)
        {
            using var pen = new Pen(AppTheme.Accent) { Width = 1.5F };
            using var path = AppTheme.RoundedRect(rect, 3);
            g.DrawPath(pen, path);
        }

        // Icon or glyph
        var iconColor = _active ? AppTheme.AccentDark : _hover ? AppTheme.Accent : AppTheme.TextPrimary;
        if (_iconDrawer is not null)
        {
            var iconRect = new Rectangle((Width - AppTheme.SmallIconSize) / 2, (Height - AppTheme.SmallIconSize) / 2, AppTheme.SmallIconSize, AppTheme.SmallIconSize);
            _iconDrawer(g, iconRect, iconColor);
        }
        else if (_glyph is not null && _glyphFont is not null)
        {
            TextRenderer.DrawText(g, _glyph, _glyphFont, rect, iconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonToggleButton — stateful toggle (Bold, Italic, etc.)
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonToggleButton : Button
{
    private readonly bool _nativeRendering = true;
    private bool _hover;
    private bool _pressed;
    private bool _toggled;
    private readonly string _glyph;
    private readonly Font _glyphFont;

    public bool Toggled
    {
        get => _toggled;
        set { if (_toggled != value) { _toggled = value; Invalidate(); ToggledChanged?.Invoke(this, value); } }
    }

    public event EventHandler<bool>? ToggledChanged;

    public RibbonToggleButton(string glyph, Font? glyphFont = null, string? tooltip = null)
    {
        _glyph = glyph;
        _glyphFont = glyphFont ?? AppTheme.GetFont(10F);
        Text = glyph;
        Width = 30;
        Height = 40;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        if (tooltip is not null)
        {
            var tt = new ToolTip();
            tt.SetToolTip(this, tooltip);
        }
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        // Background
        if (_nativeRendering) { base.OnPaint(e); return; }
        Color bgColor = _toggled ? AppTheme.RibbonBtnActiveBg
            : _pressed ? AppTheme.SurfacePressed
            : _hover ? AppTheme.AccentLighter
            : Color.Transparent;

        if (bgColor != Color.Transparent)
        {
            using var brush = AppTheme.Brush(bgColor);
            using var path = AppTheme.RoundedRect(rect, 3);
            g.FillPath(brush, path);
        }

        // Bottom indicator when toggled
        if (_toggled)
        {
            using var pen = new Pen(AppTheme.Accent) { Width = 1.5F };
            g.DrawLine(pen, 4, Height - 2, Width - 5, Height - 2);
        }

        // Glyph text
        var textColor = _toggled ? AppTheme.AccentDark : _hover ? AppTheme.TextPrimary : AppTheme.TextSecondary;
        TextRenderer.DrawText(g, _glyph, _glyphFont, rect, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonSeparator — thin vertical line between controls
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonSeparator : Control
{
    public RibbonSeparator(int height = AppTheme.LargeButtonHeight)
    {
        Width = 9;
        Height = height;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        using var pen = AppTheme.Pen(AppTheme.BorderSubtle);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.DrawLine(pen, Width / 2, AppTheme.SpaceM, Width / 2, Height - AppTheme.SpaceM);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonComboBox — modern styled combo box
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonComboBoxWrapper : Control
{
    private readonly ComboBox _combo;

    public RibbonComboBoxWrapper(ComboBox combo, int width = AppTheme.ComboBoxWidth)
    {
        _combo = combo;
        Width = width;
        Height = AppTheme.ButtonHeight;
        BackColor = AppTheme.RibbonBackground;

        combo.FlatStyle = FlatStyle.Flat;
        combo.Font = AppTheme.GetFont(8.5F);
        combo.ForeColor = AppTheme.TextPrimary;
        combo.BackColor = AppTheme.Surface;
        combo.Dock = DockStyle.Fill;
        Controls.Add(combo);
    }

    public ComboBox ComboBox => _combo;
}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonSlider — wrapped TrackBar with labels
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonSlider : Control
{
    private readonly TrackBar _trackBar;
    private readonly Label _label;
    private readonly string _caption;

    public RibbonSlider(string caption, TrackBar trackBar, int width = 180)
    {
        _caption = caption;
        _trackBar = trackBar;
        Width = width;
        Height = 50;
        BackColor = AppTheme.RibbonBackground;

        _label = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 16,
            Font = AppTheme.GetFont(7.5F),
            ForeColor = AppTheme.TextTertiary,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = AppTheme.RibbonBackground
        };

        trackBar.Dock = DockStyle.Fill;
        trackBar.TickFrequency = 25;
        trackBar.LargeChange = 10;
        trackBar.SmallChange = 5;

        Controls.Add(trackBar);
        Controls.Add(_label);
    }

    public TrackBar TrackBar => _trackBar;
}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonSegmentedControl — segmented selection (aspect ratio, etc.)
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonSegmentedControl : Control
{
    private readonly List<string> _items = [];
    private int _selectedIndex;
    private int _hoverIndex = -1;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set { _selectedIndex = value; Invalidate(); }
    }

    public event EventHandler<int>? SelectionChanged;

    public RibbonSegmentedControl(string[] items, int selectedIndex = 0)
    {
        _items.AddRange(items);
        _selectedIndex = selectedIndex;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Height = AppTheme.ButtonHeight;
        Width = items.Sum(item => AppTheme.MeasureText(item, AppTheme.GetFont(8F))) + items.Length * AppTheme.SpaceL + AppTheme.SpaceL;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var idx = HitTest(e.Location);
        if (idx != _hoverIndex) { _hoverIndex = idx; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e) { _hoverIndex = -1; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var idx = HitTest(e.Location);
        if (idx >= 0 && idx < _items.Count)
        {
            _selectedIndex = idx;
            Invalidate();
            SelectionChanged?.Invoke(this, idx);
        }
    }

    private int HitTest(Point location)
    {
        var x = AppTheme.SpaceXS;
        for (var i = 0; i < _items.Count; i++)
        {
            var w = AppTheme.MeasureText(_items[i], AppTheme.GetFont(8F)) + AppTheme.SpaceL;
            if (location.X >= x && location.X <= x + w) return i;
            x += w;
        }
        return -1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var font = AppTheme.GetFont(8F);

        // Outer border
        var outerRect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = AppTheme.RoundedRect(outerRect, 3))
        {
            using var borderPen = AppTheme.Pen(AppTheme.Border);
            g.DrawPath(borderPen, path);
        }

        var x = AppTheme.SpaceXXS;
        for (var i = 0; i < _items.Count; i++)
        {
            var w = AppTheme.MeasureText(_items[i], font) + AppTheme.SpaceL;
            var itemRect = new Rectangle(x, AppTheme.SpaceXXS, w, Height - AppTheme.SpaceXS * 2);

            if (i == _selectedIndex)
            {
                using var brush = AppTheme.Brush(AppTheme.Accent);
                using var path = AppTheme.RoundedRect(itemRect, 2);
                g.FillPath(brush, path);
                TextRenderer.DrawText(g, _items[i], font, itemRect, AppTheme.TextOnAccent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            else if (i == _hoverIndex)
            {
                using var brush = AppTheme.Brush(AppTheme.AccentLighter);
                using var path = AppTheme.RoundedRect(itemRect, 2);
                g.FillPath(brush, path);
                TextRenderer.DrawText(g, _items[i], font, itemRect, AppTheme.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            else
            {
                TextRenderer.DrawText(g, _items[i], font, itemRect, AppTheme.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            // Vertical separator between items
            if (i < _items.Count - 1)
            {
                using var pen = AppTheme.Pen(AppTheme.BorderSubtle);
                g.DrawLine(pen, x + w, 4, x + w, Height - 5);
            }

            x += w;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RibbonField — labeled input wrapper (caption above)
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonField : Control
{
    public RibbonField(string label, Control input, int width = 100)
    {
        Width = width;
        Height = 46;
        BackColor = AppTheme.RibbonBackground;

        var caption = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 16,
            Font = AppTheme.GetFont(7F),
            ForeColor = AppTheme.TextTertiary,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = AppTheme.RibbonBackground
        };

        input.Dock = DockStyle.Top;
        input.Height = AppTheme.ButtonHeight;

        Controls.Add(input);
        Controls.Add(caption);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Delegate type for icon drawing
// ═══════════════════════════════════════════════════════════════════════

internal delegate void IconDrawer(Graphics g, Rectangle bounds, Color color);

// ═══════════════════════════════════════════════════════════════════════
//  RibbonColorButton — font/highlight color picker button
// ═══════════════════════════════════════════════════════════════════════

internal sealed class RibbonColorButton : Button
{
    private readonly bool _nativeRendering = true;
    private bool _hover;
    private bool _pressed;
    private readonly Font _iconFont;
    private Color _colorSwatch;
    private readonly string _icon;

    public Color ColorSwatch
    {
        get => _colorSwatch;
        set { _colorSwatch = value; Invalidate(); }
    }

    public RibbonColorButton(string icon, Color swatch, Font? iconFont = null)
    {
        _icon = icon;
        Text = icon;
        _colorSwatch = swatch;
        _iconFont = iconFont ?? AppTheme.GetFont(12F, FontStyle.Bold);
        Width = 30;
        Height = 40;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        // Background
        if (_nativeRendering) { base.OnPaint(e); return; }
        Color bgColor = _pressed ? AppTheme.SurfacePressed
            : _hover ? AppTheme.AccentLighter
            : Color.Transparent;
        if (bgColor != Color.Transparent)
        {
            using var brush = AppTheme.Brush(bgColor);
            using var path = AppTheme.RoundedRect(rect, 3);
            g.FillPath(brush, path);
        }

        // Icon text
        var iconRect = new Rectangle(0, 1, Width, Height - 10);
        TextRenderer.DrawText(g, _icon, _iconFont, iconRect, AppTheme.TextPrimary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        // Color swatch bar at bottom
        var swatchRect = new Rectangle(4, Height - 6, Width - 9, 4);
        using (var brush = AppTheme.Brush(_colorSwatch))
            g.FillRectangle(brush, swatchRect);
        using (var border = AppTheme.Pen(AppTheme.Border))
            g.DrawRectangle(border, swatchRect);
    }
}

internal enum RibbonPresetKind
{
    TextColor,
    NoAnimation,
    CrossFade,
    ZoomIn,
    ZoomOut
}

/// <summary>
/// A large ribbon preview button used for text-colour and transition presets.
/// The preview is deliberately visual so its size and affordance match the editor ribbon.
/// </summary>
internal sealed class RibbonPresetButton : Button
{
    private readonly bool _nativeRendering = true;
    private bool _hover;
    private bool _pressed;
    private bool _active;
    private readonly RibbonPresetKind _kind;
    private readonly Color _previewColor;

    public bool Active
    {
        get => _active;
        set { if (_active != value) { _active = value; Invalidate(); } }
    }

    public RibbonPresetButton(RibbonPresetKind kind, Color previewColor, string? tooltip = null)
    {
        _kind = kind;
        Text = kind == RibbonPresetKind.TextColor ? "A" : kind switch
        {
            RibbonPresetKind.NoAnimation => "None",
            RibbonPresetKind.CrossFade => "Fade",
            RibbonPresetKind.ZoomIn => "Zoom +",
            _ => "Zoom -"
        };
        _previewColor = previewColor;
        Width = 42;
        Height = 56;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        if (tooltip is not null)
        {
            var toolTip = new ToolTip();
            toolTip.SetToolTip(this, tooltip);
        }
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var body = new Rectangle(0, 0, Width - 1, Height - 1);
        var background = _active ? Color.FromArgb(218, 218, 218)
            : _pressed ? AppTheme.SurfacePressed
            : _hover ? AppTheme.AccentLighter
            : Color.Transparent;
        if (background != Color.Transparent)
        {
            using var brush = AppTheme.Brush(background);
            using var path = AppTheme.RoundedRect(body, 3);
            g.FillPath(brush, path);
        }
        if (_active || _hover)
        {
            using var pen = AppTheme.Pen(_active ? Color.FromArgb(160, 160, 160) : AppTheme.Border);
            using var path = AppTheme.RoundedRect(body, 3);
            g.DrawPath(pen, path);
        }

        if (_kind == RibbonPresetKind.TextColor)
            DrawTextColorPreview(g);
        else
            DrawAnimationPreview(g);
    }

    private void DrawTextColorPreview(Graphics graphics)
    {
        var preview = new Rectangle(2, 2, Width - 5, Height - 5);
        using var shadow = AppTheme.Brush(_previewColor.GetBrightness() > .65F ? Color.FromArgb(95, 0, 0, 0) : Color.FromArgb(110, 255, 255, 255));
        using var foreground = AppTheme.Brush(_previewColor);
        using var font = AppTheme.GetFont(30F, FontStyle.Bold);
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString("A", font, shadow, new Rectangle(preview.X + 1, preview.Y + 1, preview.Width, preview.Height), format);
        graphics.DrawString("A", font, foreground, preview, format);
    }

    private void DrawAnimationPreview(Graphics graphics)
    {
        var accent = _active ? AppTheme.AccentDark : _previewColor;
        var frame = new Rectangle(6, 13, Width - 13, 25);
        using var border = AppTheme.Pen(accent, 1.4F);
        using var fill = AppTheme.Brush(Color.FromArgb(225, 233, 246));
        switch (_kind)
        {
            case RibbonPresetKind.NoAnimation:
                graphics.FillRectangle(fill, frame);
                graphics.DrawRectangle(border, frame);
                break;
            case RibbonPresetKind.CrossFade:
                var back = new Rectangle(frame.X - 3, frame.Y - 3, frame.Width, frame.Height);
                using (var faded = AppTheme.Brush(Color.FromArgb(110, accent))) graphics.FillRectangle(faded, back);
                graphics.DrawRectangle(border, back);
                graphics.FillRectangle(fill, frame);
                graphics.DrawRectangle(border, frame);
                break;
            case RibbonPresetKind.ZoomIn:
                graphics.DrawRectangle(border, frame);
                graphics.DrawRectangle(border, Rectangle.Inflate(frame, -5, -5));
                DrawArrow(graphics, border, new Point(frame.X + 3, frame.Y + 3), new Point(frame.X + 9, frame.Y + 9));
                DrawArrow(graphics, border, new Point(frame.Right - 3, frame.Bottom - 3), new Point(frame.Right - 9, frame.Bottom - 9));
                break;
            case RibbonPresetKind.ZoomOut:
                graphics.DrawRectangle(border, frame);
                graphics.DrawRectangle(border, Rectangle.Inflate(frame, -5, -5));
                DrawArrow(graphics, border, new Point(frame.X + 9, frame.Y + 9), new Point(frame.X + 3, frame.Y + 3));
                DrawArrow(graphics, border, new Point(frame.Right - 9, frame.Bottom - 9), new Point(frame.Right - 3, frame.Bottom - 3));
                break;
        }
    }

    private static void DrawArrow(Graphics graphics, Pen pen, Point from, Point to)
    {
        graphics.DrawLine(pen, from, to);
        var dx = Math.Sign(to.X - from.X) * 3;
        var dy = Math.Sign(to.Y - from.Y) * 3;
        graphics.DrawLine(pen, to, new Point(to.X - dx, to.Y));
        graphics.DrawLine(pen, to, new Point(to.X, to.Y - dy));
    }
}
