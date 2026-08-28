using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ChurchProjector;

internal sealed class GalleryBrowserForm : Form
{
    private readonly string _kind;
    private List<BackgroundAsset> _allAssets = [];
    private Guid? _selectedId;
    private BackgroundAsset? _selectedAsset;
    private readonly FlowLayoutPanel _grid;
    private readonly TextBox _searchBox;
    private readonly Button _deleteBtn;
    private readonly CheckBox _keepOpened;
    private readonly TrackBar _luminosity;
    private readonly Label _luminosityMinus;
    private readonly Label _luminosityPlus;
    private int _thumbGeneration;

    public event Action<BackgroundAsset?>? AssetPicked;
    public event Action? ImportRequested;
    public event Action<BackgroundAsset>? DeleteRequested;
    public event Action? ExploreRequested;
    public event Action? RebuildRequested;
    public event Action<int>? BrightnessChanged;

    public GalleryBrowserForm(string kind, IEnumerable<BackgroundAsset> assets, Guid? selectedId, int brightness)
    {
        _kind = kind;
        _selectedId = selectedId;
        _allAssets = assets.OrderBy(a => a.Name).ToList();
        Text = kind == "Video" ? "Video Gallery" : "Image Gallery";
        Size = new Size(1020, 680);
        MinimumSize = new Size(820, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        ShowInTaskbar = false;

        _grid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.White,
            Padding = new Padding(10, 10, 10, 10),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(_grid);

        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 246,
            BackColor = Color.White,
            Padding = new Padding(10, 8, 10, 10)
        };
        bottom.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(220, 226, 232)); e.Graphics.DrawLine(pen, 0, 0, bottom.Width, 0); };
        Controls.Add(bottom);

        var searchIcon = new Label { Text = "🔍", AutoSize = true, Location = new Point(12, 12), Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(90, 100, 114) };
        var searchLabel = new Label { Text = "Search", AutoSize = true, Location = new Point(30, 12), ForeColor = Color.FromArgb(70, 84, 100) };
        _searchBox = new TextBox { Location = new Point(88, 9), Height = 24, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = kind == "Video" ? "Keywords to search in the videos" : "Keywords to search in the images" };
        _searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _searchBox.TextChanged += (_, _) => RefreshGrid();
        bottom.Controls.Add(searchIcon);
        bottom.Controls.Add(searchLabel);
        bottom.Controls.Add(_searchBox);

        _deleteBtn = CreateActionButton(kind == "Video" ? "Delete selected video" : "Delete selected image", "✕", new Point(10, 68), bottom.Width - 20, enabled: false);
        _deleteBtn.Click += (_, _) => { if (_selectedAsset is not null) DeleteRequested?.Invoke(_selectedAsset); };
        var importBtn = CreateActionButton(kind == "Video" ? "Import videos" : "Import images", "＋", new Point(10, 40), bottom.Width - 20, enabled: true);
        importBtn.ForeColor = Color.FromArgb(22, 110, 45);
        importBtn.Click += (_, _) => ImportRequested?.Invoke();
        var exploreBtn = CreateActionButton(kind == "Video" ? "Explore video folder" : "Explore image folder", "📁", new Point(10, 96), bottom.Width - 20, enabled: true);
        exploreBtn.Click += (_, _) => ExploreRequested?.Invoke();
        var rebuildBtn = CreateActionButton("Rebuild thumbnails", "↻", new Point(10, 124), bottom.Width - 20, enabled: true);
        rebuildBtn.Click += (_, _) => RebuildRequested?.Invoke();

        bottom.Controls.Add(importBtn);
        bottom.Controls.Add(_deleteBtn);
        bottom.Controls.Add(exploreBtn);
        bottom.Controls.Add(rebuildBtn);

        var lumIcon = new Label { Text = "☀", AutoSize = true, Location = new Point(12, 156), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(200, 160, 40) };
        var lumLabel = new Label { Text = "Luminosity:", AutoSize = true, Location = new Point(30, 158), ForeColor = Color.FromArgb(70, 84, 100) };
        _luminosityMinus = new Label { Text = "−", AutoSize = true, Location = new Point(110, 158), ForeColor = Color.FromArgb(110, 120, 132) };
        _luminosity = new TrackBar { Minimum = -75, Maximum = 75, Value = Math.Clamp(brightness, -75, 75), TickFrequency = 25, SmallChange = 5, LargeChange = 15, Location = new Point(126, 152), Height = 30, AutoSize = false, Width = 200 };
        _luminosity.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _luminosityPlus = new Label { Text = "+", AutoSize = true, Location = new Point(330, 158), ForeColor = Color.FromArgb(110, 120, 132), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _luminosity.ValueChanged += (_, _) => BrightnessChanged?.Invoke(_luminosity.Value);
        bottom.Controls.Add(lumIcon);
        bottom.Controls.Add(lumLabel);
        bottom.Controls.Add(_luminosityMinus);
        bottom.Controls.Add(_luminosity);
        bottom.Controls.Add(_luminosityPlus);

        _keepOpened = new CheckBox { Text = "Keep the gallery opened", Checked = true, AutoSize = true, Location = new Point(12, 186), ForeColor = Color.FromArgb(70, 84, 100) };
        bottom.Controls.Add(_keepOpened);

        var closeBtn = new Button
        {
            Text = "✕  Close",
            Location = new Point(12, 212),
            Size = new Size(90, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(160, 30, 30),
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderColor = Color.FromArgb(220, 140, 140), BorderSize = 1 }
        };
        closeBtn.Click += (_, _) => Close();
        bottom.Controls.Add(closeBtn);

        Shown += (_, _) => UpdateSearchBoxWidth(bottom);
        bottom.Resize += (_, _) => UpdateSearchBoxWidth(bottom);
        _grid.Resize += (_, _) => _grid.Invalidate();

        RefreshGrid();
        UpdateDeleteButton();
        UpdateLuminosityLabels();
        _luminosity.ValueChanged += (_, _) => UpdateLuminosityLabels();
    }

    private void UpdateSearchBoxWidth(Panel bottom)
    {
        _searchBox.Width = bottom.ClientSize.Width - 100;
        _luminosity.Width = Math.Max(80, bottom.ClientSize.Width - 148);
        _luminosityPlus.Location = new Point(bottom.ClientSize.Width - 20, 158);
    }

    private static Button CreateActionButton(string text, string icon, Point location, int width, bool enabled)
    {
        var btn = new Button
        {
            Text = $"  {icon}   {text}",
            Location = location,
            Size = new Size(width, 24),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(60, 72, 86),
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderColor = Color.FromArgb(217, 222, 231), BorderSize = 1 },
            UseVisualStyleBackColor = false,
            Enabled = enabled,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        return btn;
    }

    private void UpdateDeleteButton()
    {
        _deleteBtn.Enabled = _selectedAsset is not null;
    }

    private void UpdateLuminosityLabels()
    {
        _luminosityMinus.ForeColor = _luminosity.Value <= -60 ? Color.FromArgb(65, 105, 225) : Color.FromArgb(110, 120, 132);
        _luminosityPlus.ForeColor = _luminosity.Value >= 60 ? Color.FromArgb(65, 105, 225) : Color.FromArgb(110, 120, 132);
    }

    public void SetBrightness(int value)
    {
        var clamped = Math.Clamp(value, _luminosity.Minimum, _luminosity.Maximum);
        if (_luminosity.Value != clamped) _luminosity.Value = clamped;
    }

    public void SetAssets(IEnumerable<BackgroundAsset> assets, Guid? selectedId)
    {
        _allAssets = assets.OrderBy(a => a.Name).ToList();
        _selectedId = selectedId;
        _selectedAsset = _selectedId is Guid id ? _allAssets.FirstOrDefault(a => a.Id == id) : null;
        RefreshGrid();
        UpdateDeleteButton();
    }

    public void RefreshGrid()
    {
        _thumbGeneration++;
        var generation = _thumbGeneration;
        var filter = _searchBox.Text.Trim();
        IEnumerable<BackgroundAsset> filtered = string.IsNullOrEmpty(filter)
            ? _allAssets
            : _allAssets.Where(a => a.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        var list = filtered.ToList();

        foreach (Control c in _grid.Controls) { if (c is Panel p) foreach (Control inner in p.Controls) if (inner is PictureBox pb && pb.Image is not null) { var img = pb.Image; pb.Image = null; img.Dispose(); } }
        _grid.SuspendLayout();
        _grid.Controls.Clear();

        _grid.Controls.Add(CreateItem(null));

        foreach (var asset in list)
        {
            _grid.Controls.Add(CreateItem(asset));
        }
        _grid.ResumeLayout(true);

        foreach (Control c in _grid.Controls)
        {
            if (c is not Panel item) continue;
            var asset = item.Tag as BackgroundAsset;
            var pic = item.Controls.OfType<PictureBox>().FirstOrDefault();
            if (pic is null) continue;
            if (asset is null)
            {
                pic.BackColor = Color.Black;
                continue;
            }
            var capturedPic = pic;
            var capturedAsset = asset;
            var gen = generation;
            var ui = SynchronizationContext.Current;
            Task.Run(() =>
            {
                var thumb = CreateThumb(capturedAsset, 260, 156);
                void Apply()
                {
                    if (gen != _thumbGeneration || IsDisposed || capturedPic.IsDisposed) { thumb?.Dispose(); return; }
                    if (capturedPic.Image is not null) { var old = capturedPic.Image; capturedPic.Image = null; old.Dispose(); }
                    capturedPic.Image = thumb;
                }
                if (ui is not null) ui.Post(_ => Apply(), null);
                else thumb?.Dispose();
            });
        }
    }

    private Panel CreateItem(BackgroundAsset? asset)
    {
        var isSelected = asset is null ? _selectedId is null : asset.Id == _selectedId;
        var panel = new Panel
        {
            Width = 148,
            Height = 120,
            Margin = new Padding(6),
            BackColor = isSelected ? Color.FromArgb(232, 238, 255) : Color.White,
            Cursor = Cursors.Hand,
            Tag = asset
        };
        panel.Paint += (s, e) =>
        {
            if (isSelected)
            {
                using var pen = new Pen(Color.FromArgb(65, 105, 225), 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
            else
            {
                using var pen = new Pen(Color.FromArgb(220, 226, 232));
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
        };
        var pic = new PictureBox
        {
            Size = new Size(132, 78),
            Location = new Point(8, 8),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = asset is null ? Color.Black : Color.FromArgb(245, 247, 249),
            BorderStyle = BorderStyle.None,
            Cursor = Cursors.Hand
        };
        var label = new Label
        {
            Text = asset is null ? "Without image" : asset.Name,
            Location = new Point(4, 92),
            Size = new Size(140, 22),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            ForeColor = Color.FromArgb(55, 65, 80),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };
        panel.Controls.Add(pic);
        panel.Controls.Add(label);
        void Pick()
        {
            _selectedId = asset?.Id;
            _selectedAsset = asset;
            foreach (Control c in _grid.Controls) c.Invalidate();
            UpdateDeleteButton();
            AssetPicked?.Invoke(asset);
            if (!_keepOpened.Checked) Close();
        }
        panel.Click += (_, _) => Pick();
        pic.Click += (_, _) => Pick();
        label.Click += (_, _) => Pick();
        return panel;
    }

    public void RebuildThumbnails()
    {
        RefreshGrid();
    }

    private static Image? CreateThumb(BackgroundAsset asset, int w, int h)
    {
        try
        {
            if (asset.Kind == "Video")
            {
                if (!File.Exists(asset.FilePath)) return Placeholder(true, w, h);
                var shell = ShellThumbnail(asset.FilePath, w, h, true) ?? ShellThumbnail(asset.FilePath, w, h, false);
                if (shell is null) return Placeholder(true, w, h);
                using (shell) return Fit(shell, w, h);
            }
            if (!File.Exists(asset.FilePath)) return Placeholder(false, w, h);
            using var src = Image.FromFile(asset.FilePath);
            return Fit(src, w, h);
        }
        catch { return Placeholder(asset.Kind == "Video", w, h); }
    }

    private static Bitmap Fit(Image src, int w, int h)
    {
        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var scale = Math.Min((double)w / Math.Max(1, src.Width), (double)h / Math.Max(1, src.Height));
        var nw = Math.Max(1, (int)Math.Round(src.Width * scale));
        var nh = Math.Max(1, (int)Math.Round(src.Height * scale));
        g.Clear(Color.Transparent);
        g.DrawImage(src, (w - nw) / 2, (h - nh) / 2, nw, nh);
        return bmp;
    }

    private static Bitmap Placeholder(bool video, int w, int h)
    {
        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        g.Clear(video ? Color.FromArgb(40, 46, 56) : Color.FromArgb(230, 234, 238));
        if (video)
        {
            using var brush = new SolidBrush(Color.White);
            var cx = w / 2f; var cy = h / 2f;
            g.FillPolygon(brush, [new PointF(cx - 10, cy - 14), new PointF(cx - 10, cy + 14), new PointF(cx + 16, cy)]);
        }
        return bmp;
    }

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig] int GetImage(NativeSize size, int flags, out IntPtr bitmap);
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeSize { public int Width; public int Height; }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(string path, IntPtr pbc, in Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory factory);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    private static Image? ShellThumbnail(string path, int w, int h, bool thumb)
    {
        try
        {
            SHCreateItemFromParsingName(path, IntPtr.Zero, new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), out var f);
            var r = f.GetImage(new NativeSize { Width = w, Height = h }, thumb ? 8 : 4, out var bmp);
            if (r != 0 || bmp == IntPtr.Zero) return null;
            try { using var tmp = Image.FromHbitmap(bmp); return new Bitmap(tmp); } finally { DeleteObject(bmp); }
        }
        catch { return null; }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (Control c in _grid.Controls)
                if (c is Panel p) foreach (Control inner in p.Controls) if (inner is PictureBox pb && pb.Image is not null) { pb.Image.Dispose(); pb.Image = null; }
        }
        base.Dispose(disposing);
    }
}
