using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ChurchProjector;

internal sealed class BackgroundGalleryPanel : Panel
{
    public event Action<BackgroundAsset?>? AssetSelected;
    public event Action? ExpandRequested;

    public BackgroundGalleryPanel(string caption)
    {
        BackColor = Color.White;
        Width = 320;
        Height = 108;
        var gallery = new BackgroundGallery { Bounds = new Rectangle(0, 0, 320, 92), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom, WithoutLabel = caption == "Video" ? "Without video" : "Without image" };
        var label = new Label { Text = caption, Bounds = new Rectangle(0, 92, 320, 15), Anchor = AnchorStyles.Bottom, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(102, 112, 133), Font = new Font("Segoe UI", 8.25F), BackColor = Color.White, Margin = new Padding(0) };
        Controls.Add(gallery);
        Controls.Add(label);
        gallery.AssetSelected += asset => AssetSelected?.Invoke(asset);
        gallery.ExpandRequested += () => ExpandRequested?.Invoke();
        gallery.EmptyText = $"No {caption.ToLowerInvariant()}s imported yet";
    }

    public void SetAssets(IEnumerable<BackgroundAsset> assets) => ((BackgroundGallery)Controls[0]).SetAssets(assets);

    public void SelectAsset(Guid? id) => ((BackgroundGallery)Controls[0]).SelectAsset(id);
}

internal sealed class BackgroundGallery : Control
{
    private sealed class Entry
    {
        public BackgroundAsset? Asset;
        public Image? Thumb;
    }

    private const int CellWidth = 42;
    private const int CellHeight = 42;
    private const int ThumbWidth = 38;
    private const int ThumbHeight = 28;
    private const int ArrowWidth = 17;
    private const int Pad = 3;

    private readonly List<Entry> _entries = [];
    private readonly ToolTip _toolTip = new();
    private int _generation;
    private int _scrollRow;
    private int _hoverCell = -1;
    private bool _upHot;
    private bool _downHot;
    private bool _expandHot;
    private Guid? _selectedId;
    private static readonly SemaphoreSlim _thumbGate = new(4);

    public event Action<BackgroundAsset?>? AssetSelected;
    public event Action? ExpandRequested;

    public string EmptyText { get; set; } = "";
    public string WithoutLabel { get; set; } = "Without image";

    public BackgroundGallery()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Color.White;
        TabStop = false;
    }

    private int Columns => Math.Max(1, (Width - ArrowWidth - Pad * 2) / CellWidth);
    private int VisibleRows => Math.Max(1, (Height - Pad * 2) / CellHeight);
    private int TotalRows => (_entries.Count + Columns - 1) / Columns;
    private int MaxScrollRow => Math.Max(0, TotalRows - VisibleRows);
    private int FirstVisibleIndex => _scrollRow * Columns;
    private const int ExpandHeight = 14;
    private Rectangle UpArrowRect => new(Width - ArrowWidth, 1, ArrowWidth - 1, (Height - ExpandHeight - 2) / 2);
    private Rectangle DownArrowRect => new(Width - ArrowWidth, 1 + (Height - ExpandHeight - 2) / 2 + 1, ArrowWidth - 1, (Height - ExpandHeight - 2) / 2);
    private Rectangle ExpandRect => new(Width - ArrowWidth, Height - ExpandHeight, ArrowWidth - 1, ExpandHeight - 1);

    public void SetAssets(IEnumerable<BackgroundAsset> assets)
    {
        _generation++;
        var generation = _generation;
        var newList = assets.ToList();
        var withoutEntry = new Entry { Asset = null, Thumb = CreateWithoutThumb() };
        var newIds = new HashSet<Guid>(newList.Select(a => a.Id));
        var toRemove = _entries.Where(e => e.Asset is not null && !newIds.Contains(e.Asset.Id)).ToList();
        foreach (var r in toRemove) { r.Thumb?.Dispose(); _entries.Remove(r); }
        var map = _entries.Where(e => e.Asset is not null).ToDictionary(e => e.Asset!.Id);
        var hasWithout = _entries.Any(e => e.Asset is null);
        var merged = new List<Entry>(newList.Count + 1);
        if (!hasWithout)
            merged.Add(withoutEntry);
        else
        {
            var existingWithout = _entries.First(e => e.Asset is null);
            withoutEntry.Thumb?.Dispose();
            merged.Add(existingWithout);
        }
        foreach (var asset in newList)
        {
            if (map.TryGetValue(asset.Id, out var existing))
            {
                existing.Asset = asset;
                merged.Add(existing);
            }
            else
            {
                merged.Add(new Entry { Asset = asset });
            }
        }
        foreach (var leftover in _entries.Where(e => !merged.Contains(e)).ToList()) { if (leftover.Asset is not null) leftover.Thumb?.Dispose(); }
        _entries.Clear();
        _entries.AddRange(merged);
        _scrollRow = Math.Clamp(_scrollRow, 0, MaxScrollRow);
        _hoverCell = -1;
        var ui = SynchronizationContext.Current;
        var visibleEnd = Math.Min(_entries.Count, FirstVisibleIndex + Columns * VisibleRows + Columns);
        var ordered = _entries.Select((e, i) => (e, i)).OrderBy(t => t.i < FirstVisibleIndex || t.i >= visibleEnd ? 1 : 0).Select(t => t.e).ToList();
        foreach (var entry in ordered.Where(e => e.Thumb is null && e.Asset is not null))
        {
            Task.Run(async () =>
            {
                await _thumbGate.WaitAsync();
                Image? thumb = null;
                try { thumb = CreateThumbnail(entry.Asset!); } finally { _thumbGate.Release(); }
                void Apply()
                {
                    if (generation != _generation || IsDisposed || entry.Thumb is not null) { thumb?.Dispose(); return; }
                    entry.Thumb = thumb;
                    Invalidate();
                }
                if (ui is { } context) context.Post(_ => Apply(), null);
                else thumb?.Dispose();
            });
        }
        Invalidate();
    }

    private Image CreateWithoutThumb()
    {
        var bmp = new Bitmap(ThumbWidth * 2, ThumbHeight * 2);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Black);
        return bmp;
    }

    public void SelectAsset(Guid? id)
    {
        if (_selectedId == id) return;
        _selectedId = id;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);
        using var border = new Pen(Color.FromArgb(217, 222, 231));
        g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        g.DrawLine(border, Width - ArrowWidth, 1, Width - ArrowWidth, Height - 1);
        if (_entries.Count == 0 && !string.IsNullOrEmpty(EmptyText))
        {
            using var font = new Font("Segoe UI", 8.25F);
            using var brush = new SolidBrush(Color.FromArgb(152, 162, 179));
            var size = g.MeasureString(EmptyText, font);
            g.DrawString(EmptyText, font, brush, (Width - ArrowWidth - size.Width) / 2f, (Height - size.Height) / 2f);
        }
        var visible = Columns * VisibleRows;
        for (var i = FirstVisibleIndex; i < Math.Min(_entries.Count, FirstVisibleIndex + visible); i++) DrawCell(g, i);
        DrawArrow(g, UpArrowRect, up: true, enabled: _scrollRow > 0, hot: _upHot);
        DrawArrow(g, DownArrowRect, up: false, enabled: _scrollRow < MaxScrollRow, hot: _downHot);
        DrawExpand(g, ExpandRect, hot: _expandHot);
    }

    private void DrawCell(Graphics g, int index)
    {
        var entry = _entries[index];
        var rect = CellRect(index);
        var selected = entry.Asset is null ? _selectedId is null : entry.Asset.Id == _selectedId;
        if (selected || index == _hoverCell)
        {
            using var fill = new SolidBrush(selected ? Color.FromArgb(232, 238, 255) : Color.FromArgb(243, 246, 255));
            g.FillRectangle(fill, rect);
        }
        var thumbRect = ThumbRect(rect);
        var thumb = entry.Thumb;
        if (thumb is not null)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(thumb, thumbRect);
        }
        else if (entry.Asset is not null)
        {
            using var placeholder = new SolidBrush(entry.Asset.Kind == "Video" ? Color.FromArgb(52, 58, 70) : Color.FromArgb(238, 241, 245));
            g.FillRectangle(placeholder, thumbRect);
        }
        else
        {
            using var placeholder = new SolidBrush(Color.Black);
            g.FillRectangle(placeholder, thumbRect);
        }
        if (selected)
        {
            using var pen = new Pen(Color.FromArgb(65, 105, 225));
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }
    }

    private static void DrawArrow(Graphics g, Rectangle bounds, bool up, bool enabled, bool hot)
    {
        if (hot && enabled)
        {
            using var fill = new SolidBrush(Color.FromArgb(232, 238, 255));
            g.FillRectangle(fill, bounds);
        }
        using var pen = new Pen(Color.FromArgb(217, 222, 231));
        if (up) g.DrawLine(pen, bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom);
        else g.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right, bounds.Top);
        using var brush = new SolidBrush(enabled ? Color.FromArgb(102, 112, 133) : Color.FromArgb(207, 215, 224));
        var cx = bounds.Left + bounds.Width / 2f;
        var cy = bounds.Top + bounds.Height / 2f;
        PointF[] points = up
            ? [new PointF(cx - 4, cy + 2), new PointF(cx + 4, cy + 2), new PointF(cx, cy - 3)]
            : [new PointF(cx - 4, cy - 2), new PointF(cx + 4, cy - 2), new PointF(cx, cy + 3)];
        g.FillPolygon(brush, points);
    }

    private static void DrawExpand(Graphics g, Rectangle bounds, bool hot)
    {
        if (hot)
        {
            using var fill = new SolidBrush(Color.FromArgb(232, 238, 255));
            g.FillRectangle(fill, bounds);
        }
        using var pen = new Pen(Color.FromArgb(217, 222, 231));
        g.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right, bounds.Top);
        using var brush = new SolidBrush(Color.FromArgb(102, 112, 133));
        var cx = bounds.Left + bounds.Width / 2f;
        var cy = bounds.Top + bounds.Height / 2f;
        PointF[] outer = [new PointF(cx - 4, cy - 1), new PointF(cx + 4, cy - 1), new PointF(cx, cy + 3)];
        PointF[] inner = [new PointF(cx - 4, cy - 4), new PointF(cx + 4, cy - 4), new PointF(cx, cy)];
        g.FillPolygon(brush, outer);
        g.FillPolygon(Brushes.White, inner);
        using var thin = new Pen(Color.FromArgb(102, 112, 133), 1f);
        g.DrawLine(thin, bounds.Left + 2, bounds.Top + 2, bounds.Right - 2, bounds.Top + 2);
    }

    private Rectangle CellRect(int index)
    {
        var column = index % Columns;
        var row = index / Columns - _scrollRow;
        return new Rectangle(Pad + column * CellWidth + 1, Pad + row * CellHeight + 1, CellWidth - 3, CellHeight - 3);
    }

    private static Rectangle ThumbRect(Rectangle cell)
    {
        var width = Math.Min(cell.Width - 4, ThumbWidth);
        var height = Math.Min(cell.Height - 6, ThumbHeight);
        return new Rectangle(cell.X + (cell.Width - width) / 2, cell.Y + (cell.Height - height) / 2, width, height);
    }

    private int HitCell(Point location)
    {
        if (location.X < 0 || location.Y < 0 || location.X >= Width - ArrowWidth || location.Y >= Height) return -1;
        var column = (location.X - Pad) / CellWidth;
        if (column < 0 || column >= Columns) return -1;
        var row = (location.Y - Pad) / CellHeight + _scrollRow;
        if (row < 0) return -1;
        var index = row * Columns + column;
        return index < _entries.Count ? index : -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var cell = HitCell(e.Location);
        var upHot = _scrollRow > 0 && UpArrowRect.Contains(e.Location);
        var downHot = _scrollRow < MaxScrollRow && DownArrowRect.Contains(e.Location);
        var expandHot = ExpandRect.Contains(e.Location);
        if (cell != _hoverCell)
        {
            _hoverCell = cell;
            var tip = cell >= 0 ? (_entries[cell].Asset?.Name ?? WithoutLabel) : expandHot ? "Show all backgrounds" : "";
            _toolTip.SetToolTip(this, tip);
            Invalidate();
        }
        if (upHot != _upHot || downHot != _downHot || expandHot != _expandHot)
        {
            _upHot = upHot;
            _downHot = downHot;
            _expandHot = expandHot;
            Invalidate();
        }
        Cursor = cell >= 0 || upHot || downHot || expandHot ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverCell != -1 || _upHot || _downHot || _expandHot)
        {
            _hoverCell = -1;
            _upHot = false;
            _downHot = false;
            _expandHot = false;
            _toolTip.SetToolTip(this, "");
            Invalidate();
        }
        Cursor = Cursors.Default;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button != MouseButtons.Left) return;
        if (ExpandRect.Contains(e.Location))
        {
            ExpandRequested?.Invoke();
            return;
        }
        var cell = HitCell(e.Location);
        if (cell >= 0)
        {
            var entry = _entries[cell];
            _selectedId = entry.Asset?.Id;
            Invalidate();
            AssetSelected?.Invoke(entry.Asset);
            return;
        }
        if (UpArrowRect.Contains(e.Location)) ScrollBy(-1);
        else if (DownArrowRect.Contains(e.Location)) ScrollBy(1);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        ScrollBy(-Math.Sign(e.Delta));
    }

    private void ScrollBy(int rows)
    {
        var target = Math.Clamp(_scrollRow + rows, 0, MaxScrollRow);
        if (target == _scrollRow) return;
        _scrollRow = target;
        Invalidate();
    }

    private void ClearThumbs()
    {
        foreach (var entry in _entries) entry.Thumb?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearThumbs();
            _toolTip.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Image? CreateThumbnail(BackgroundAsset asset) => ThumbnailHelper.Create(asset, ThumbWidth * 2, ThumbHeight * 2);
}
