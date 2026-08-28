using System.Diagnostics;

namespace ChurchProjector;

internal sealed class GalleryDropdownForm : Form
{
    private readonly string _kind;
    public string Kind => _kind;
    private List<BackgroundAsset> _allAssets = [];
    private Guid? _selectedId;
    private readonly FlowLayoutPanel _grid;
    private readonly TextBox _searchBox;
    private readonly Button _deleteBtn;
    private int _gen;

    public event Action<BackgroundAsset?>? AssetSelected;
    public event Action? ImportRequested;
    public event Action<BackgroundAsset>? DeleteRequested;
    public event Action? ExploreRequested;

    public GalleryDropdownForm(string kind, IEnumerable<BackgroundAsset> assets, Guid? selectedId)
    {
        _kind = kind;
        _allAssets = assets.OrderBy(a => a.Name).ToList();
        _selectedId = selectedId;
        Text = kind;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.White;
        Size = new Size(740, 420);
        MinimumSize = new Size(640, 320);
        Font = new Font("Segoe UI", 9F);
        TopLevel = true;
        ShowIcon = false;
        DoubleBuffered = true;

        var topBar = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.White, Padding = new Padding(10, 6, 10, 6) };
        topBar.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(217, 222, 231)); e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1); };
        var searchIcon = new Label { Text = "🔍", AutoSize = true, Location = new Point(6, 8), ForeColor = Color.FromArgb(90, 100, 114) };
        var searchLabel = new Label { Text = "Search", AutoSize = true, Location = new Point(24, 9), ForeColor = Color.FromArgb(70, 84, 100), Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
        _searchBox = new TextBox { Location = new Point(80, 7), Height = 22, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = kind == "Video" ? "Keywords to search in the videos" : "Keywords to search in the images", Width = 420, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        _searchBox.TextChanged += (_, _) => RefreshGrid();
        topBar.Controls.Add(searchIcon);
        topBar.Controls.Add(searchLabel);
        topBar.Controls.Add(_searchBox);
        topBar.Resize += (_, _) => _searchBox.Width = topBar.ClientSize.Width - 90;
        Controls.Add(topBar);

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Color.FromArgb(245, 247, 249), Padding = new Padding(10, 8, 10, 8) };
        bottomBar.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(217, 222, 231)); e.Graphics.DrawLine(pen, 0, 0, bottomBar.Width, 0); };
        var importBtn = new Button
        {
            Text = kind == "Video" ? "  ＋  Import videos" : "  ＋  Import images",
            Size = new Size(150, 28),
            Location = new Point(10, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(22, 110, 45),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderColor = Color.FromArgb(180, 210, 180), BorderSize = 1 },
            UseVisualStyleBackColor = false
        };
        importBtn.Click += (_, _) => ImportRequested?.Invoke();
        _deleteBtn = new Button
        {
            Text = kind == "Video" ? "  ✕  Delete video" : "  ✕  Delete image",
            Size = new Size(140, 28),
            Location = new Point(168, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(160, 40, 40),
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderColor = Color.FromArgb(220, 180, 180), BorderSize = 1 },
            UseVisualStyleBackColor = false,
            Enabled = false
        };
        _deleteBtn.Click += (_, _) =>
        {
            var sel = _allAssets.FirstOrDefault(a => a.Id == _selectedId);
            if (sel is not null) DeleteRequested?.Invoke(sel);
        };
        var exploreBtn = new Button
        {
            Text = "  📁  Open folder",
            Size = new Size(130, 28),
            Location = new Point(316, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(70, 84, 100),
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderColor = Color.FromArgb(210, 218, 227), BorderSize = 1 },
            UseVisualStyleBackColor = false
        };
        exploreBtn.Click += (_, _) => ExploreRequested?.Invoke();
        var closeBtn = new Button
        {
            Text = "✕ Close",
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(90, 100, 114),
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderColor = Color.FromArgb(210, 218, 227), BorderSize = 1 },
            UseVisualStyleBackColor = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        closeBtn.Location = new Point(bottomBar.Width - 90, 8);
        closeBtn.Click += (_, _) => Close();
        bottomBar.Resize += (_, _) => closeBtn.Left = bottomBar.ClientSize.Width - 90;
        bottomBar.Controls.Add(importBtn);
        bottomBar.Controls.Add(_deleteBtn);
        bottomBar.Controls.Add(exploreBtn);
        bottomBar.Controls.Add(closeBtn);
        Controls.Add(bottomBar);

        _grid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.White,
            Padding = new Padding(10, 10, 10, 10)
        };
        Controls.Add(_grid);
        _grid.BringToFront();

        Deactivate += (_, _) => Close();
        Shown += (_, _) => _searchBox.Focus();
        RefreshGrid();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000;
            return cp;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Color.FromArgb(180, 190, 200));
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    public void SetAssets(IEnumerable<BackgroundAsset> assets, Guid? selectedId)
    {
        _allAssets = assets.OrderBy(a => a.Name).ToList();
        _selectedId = selectedId;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        _gen++;
        var gen = _gen;
        var filter = _searchBox.Text.Trim();
        IEnumerable<BackgroundAsset> filtered = string.IsNullOrEmpty(filter) ? _allAssets : _allAssets.Where(a => a.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        var list = filtered.ToList();
        foreach (Control c in _grid.Controls)
            if (c is Panel p) foreach (Control inner in p.Controls) if (inner is PictureBox pb && pb.Image is not null) { var img = pb.Image; pb.Image = null; img.Dispose(); }
        _grid.SuspendLayout();
        _grid.Controls.Clear();
        _grid.Controls.Add(CreateItem(null));
        foreach (var a in list) _grid.Controls.Add(CreateItem(a));
        _grid.ResumeLayout(true);
        UpdateDeleteState();
        foreach (Control c in _grid.Controls)
        {
            if (c is not Panel item) continue;
            var asset = item.Tag as BackgroundAsset;
            if (asset is null) continue;
            var pic = item.Controls.OfType<PictureBox>().FirstOrDefault();
            if (pic is null) continue;
            var capturedPic = pic;
            var capturedAsset = asset;
            var ui = SynchronizationContext.Current;
            Task.Run(() =>
            {
                var thumb = ThumbnailHelper.Create(capturedAsset, 220, 132);
                void Apply()
                {
                    if (gen != _gen || IsDisposed || capturedPic.IsDisposed) { thumb?.Dispose(); return; }
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
            Width = 132,
            Height = 112,
            Margin = new Padding(5),
            BackColor = isSelected ? Color.FromArgb(214, 230, 245) : Color.White,
            Cursor = Cursors.Hand,
            Tag = asset
        };
        panel.Paint += (s, e) =>
        {
            var sel = asset is null ? _selectedId is null : asset.Id == _selectedId;
            if (sel) { using var pen = new Pen(Color.FromArgb(65, 105, 225), 1.6f); e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1); }
            else { using var pen = new Pen(Color.FromArgb(217, 222, 231)); e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1); }
        };
        var pic = new PictureBox
        {
            Size = new Size(116, 72),
            Location = new Point(8, 8),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = asset is null ? Color.Black : Color.FromArgb(245, 247, 249),
            Cursor = Cursors.Hand
        };
        var label = new Label
        {
            Text = asset is null ? "Without image" : asset.Name,
            Location = new Point(4, 86),
            Size = new Size(124, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(55, 65, 80),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };
        panel.Controls.Add(pic);
        panel.Controls.Add(label);
        void Pick()
        {
            _selectedId = asset?.Id;
            foreach (Control c in _grid.Controls) c.Invalidate();
            UpdateDeleteState();
            AssetSelected?.Invoke(asset);
            Close();
        }
        panel.Click += (_, _) => Pick();
        pic.Click += (_, _) => Pick();
        label.Click += (_, _) => Pick();
        return panel;
    }

    private void UpdateDeleteState()
    {
        _deleteBtn.Enabled = _selectedId is not null && _allAssets.Any(a => a.Id == _selectedId);
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
