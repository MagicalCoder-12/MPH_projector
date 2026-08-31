namespace ChurchProjector;

public partial class Form1
{
    private Control BuildWorkspace()
    {
        var outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 6, 6, 8), BackColor = Color.FromArgb(245, 247, 250) };

        // Left: library  | Right: editor (middle) | verses + preview (right)
        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,   
            BackColor = Color.FromArgb(245, 247, 250),
            SplitterWidth = 4
        };

        // Left panel: Song Library
        var libraryOuter = Section("Song library", "");
        var libraryContent = (TableLayoutPanel)libraryOuter.Tag!;

        // Three rows:
        // Row 0 = Search box
        // Row 1 = Sort filter
        // Row 2 = Song list
        libraryContent.RowCount = 3;
        libraryContent.RowStyles.Clear();
        libraryContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Search
        libraryContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Sort
        libraryContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Song list fills remaining space

        // Search box
        _librarySearch = new TextBox
        {
            PlaceholderText = "Search songs",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 5)
        };

        _librarySearch.TextChanged += (_, _) => FilterLibrary(_librarySearch.Text);
        libraryContent.Controls.Add(_librarySearch, 0, 0);

        // Sort filter - alphabetical default
        var sortPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 0, 5), WrapContents = false };
        sortPanel.Controls.Add(new Label { Text = "Sort:", AutoSize = true, ForeColor = Color.FromArgb(107, 118, 131), Font = new Font("Segoe UI", 8F, FontStyle.Bold), Margin = new Padding(0, 4, 6, 0) });
        _librarySort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Margin = new Padding(0, 0, 0, 0) };
        _librarySort.Items.AddRange(["Alphabetical", "Recent", "Oldest"]);
        _librarySort.SelectedIndex = 0;
        _librarySortMode = "alphabetical";
        _librarySort.SelectedIndexChanged += (_, _) =>
        {
            _librarySortMode = _librarySort.SelectedItem?.ToString()?.ToLowerInvariant() ?? "alphabetical";
            FilterLibrary(_librarySearch.Text);
        };
        sortPanel.Controls.Add(_librarySort);
        _songCountLabel = new Label { Text = "", AutoSize = true, ForeColor = Color.FromArgb(107, 118, 131), Font = new Font("Segoe UI", 8F), Margin = new Padding(8, 4, 0, 0) };
        sortPanel.Controls.Add(_songCountLabel);
        libraryContent.Controls.Add(sortPanel, 0, 1);

        // Song list
        _libraryList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 10F),
            Margin = new Padding(0)
        };

        _libraryList.DoubleClick += (_, _) =>
        {
            if (_libraryList.SelectedItem is Song song)
                LoadSong(song);
        };

        _libraryList.SelectedIndexChanged += (_, _) =>
        {
            if (!_updating && _libraryList.SelectedItem is Song s)
                LoadSong(s);
        };

        libraryContent.Controls.Add(_libraryList, 0, 2);

        // Populate the library immediately so songs are visible on startup
        FilterLibrary("");

        // Add the library section to the left panel
        mainSplit.Panel1.Controls.Add(libraryOuter);


        var rightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = Color.FromArgb(245, 247, 250),
            SplitterWidth = 4
        };

        // Middle column
        rightSplit.Panel1.Controls.Add(BuildEditorPanel());

        // Right column (Verses + Preview)
        var previewSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BackColor = Color.FromArgb(245, 247, 250),
            SplitterWidth = 4
        };

        previewSplit.Panel1.Controls.Add(BuildSlidesPanel());
        previewSplit.Panel2.Controls.Add(BuildPreviewPanel());

        rightSplit.Panel2.Controls.Add(previewSplit);

        LayoutSplit(rightSplit, rightSplit.Panel2, 0.65f);
        LayoutSplit(previewSplit, previewSplit.Panel2, 0.40f);
            
        mainSplit.Panel2.Controls.Add(rightSplit);
        outer.Controls.Add(mainSplit);
        return outer;
    }  

    private Control BuildSlidesPanel()
    {
        var outer = Section("Slides", "Select a slide to preview");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 1;
        content.RowStyles.Clear();
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _slideList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            DrawMode = DrawMode.OwnerDrawVariable,
            Font = new Font("Segoe UI", 10F),
            BackColor = Color.White,
            Margin = new Padding(0)
        };
        _slideList.MeasureItem += (_, e) =>
        {
            if (e.Index < 0 || e.Index >= _slideList.Items.Count) return;
            var text = _slideList.Items[e.Index]?.ToString() ?? string.Empty;
            var width = Math.Max(80, _slideList.ClientSize.Width - 82);
            var measured = TextRenderer.MeasureText(e.Graphics, text, _slideList.Font, new Size(width, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            e.ItemHeight = Math.Max(40, measured.Height + 10);
        };
        _slideList.DrawItem += (_, e) =>
        {
            if (e.Index < 0 || e.Index >= _slideList.Items.Count) return;
            var selected = (e.State & DrawItemState.Selected) != 0;
            using var background = new SolidBrush(selected ? AppTheme.Accent : Color.White);
            e.Graphics.FillRectangle(background, e.Bounds);
            using var divider = new Pen(selected ? Color.FromArgb(115, 150, 225) : AppTheme.BorderSubtle);
            e.Graphics.DrawLine(divider, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            var ink = selected ? Color.White : AppTheme.TextPrimary;
            var box = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top + (e.Bounds.Height - 13) / 2, 13, 13);
            using var boxPen = new Pen(selected ? Color.White : AppTheme.TextSecondary);
            e.Graphics.DrawRectangle(boxPen, box);
            e.Graphics.DrawLine(boxPen, box.Left + 2, box.Top + 6, box.Left + 5, box.Bottom - 2);
            e.Graphics.DrawLine(boxPen, box.Left + 5, box.Bottom - 2, box.Right - 2, box.Top + 2);

            var label = $"V{e.Index + 1}";
            using var labelFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, label, labelFont, new Rectangle(e.Bounds.Left + 36, e.Bounds.Top, 38, e.Bounds.Height), selected ? Color.White : AppTheme.AccentDark, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            var itemText = _slideList.Items[e.Index]?.ToString() ?? string.Empty;
            var firstSpace = itemText.IndexOf("   ", StringComparison.Ordinal);
            var lyricText = firstSpace >= 0 ? itemText[(firstSpace + 3)..] : itemText;
            TextRenderer.DrawText(e.Graphics, lyricText, _slideList.Font, new Rectangle(e.Bounds.Left + 78, e.Bounds.Top + 5, Math.Max(20, e.Bounds.Width - 86), e.Bounds.Height - 10), ink, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        };
        _slideList.SelectedIndexChanged += (_, _) => { if (!_updating && _slideList.SelectedIndex >= 0) SelectSlide(_slideList.SelectedIndex); };
        content.Controls.Add(_slideList, 0, 0);
        return outer;
    }

    private static void LayoutSplit(SplitContainer split, Control measured, float fraction)
    {
        var initialised = false;
        measured.Resize += (_, _) =>
        {
            if (initialised || measured.ClientSize.Width < 80) return;
            initialised = true;
            split.SplitterDistance = Math.Max(split.Panel1MinSize, (int)(measured.ClientSize.Width * fraction));
        };
    }

    private Control BuildAgendaPanel()
    {
        var outer = Section("Service agenda", "Songs selected for this service");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 5;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 52));

        var agendaActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false, Margin = new Padding(0) };
        var add = Button("Add song", _brand, Color.White, 72, 32);
        add.Click += (_, _) => AddCurrentSongToAgenda();
        var remove = Button("Remove", Color.White, Color.FromArgb(52, 64, 84), 56, 32);
        remove.Click += (_, _) => RemoveAgendaItem();
        var update = Button("Update", Color.White, Color.FromArgb(52, 64, 84), 55, 32);
        update.Click += (_, _) => UpdateAgendaItem();
        var up = Button("Up", Color.White, Color.FromArgb(52, 64, 84), 28, 32);
        up.Click += (_, _) => MoveAgendaItem(-1);
        var down = Button("Down", Color.White, Color.FromArgb(52, 64, 84), 36, 32);
        down.Click += (_, _) => MoveAgendaItem(1);
        agendaActions.Controls.AddRange([add, remove, update, up, down]);
        _agendaList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 7, 0, 10) };
        _agendaList.SelectedIndexChanged += (_, _) => { if (!_updating) LoadAgendaItem(); };
        var libraryTitle = SmallLabel("SONG LIBRARY");
        var search = new TextBox { PlaceholderText = "Search songs", Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 6) };
        search.TextChanged += (_, _) => FilterLibrary(search.Text);
        _libraryList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F) };
        _libraryList.DoubleClick += (_, _) => { if (_libraryList.SelectedItem is Song song) LoadSong(song); };
        content.Controls.Add(agendaActions, 0, 0);
        content.Controls.Add(_agendaList, 0, 1);
        content.Controls.Add(libraryTitle, 0, 2);
        content.Controls.Add(search, 0, 3);
        content.Controls.Add(_libraryList, 0, 4);
        FilterLibrary("");
        return outer;
    }

    private Control BuildEditorPanel()
    {
        var outer = Section("Song editor", "Edit lyrics — slides update as you type");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 5;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 135));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var songActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false, Margin = new Padding(0, 0, 0, 5) };
        var newSong = Button("New song", Color.White, Color.FromArgb(52, 64, 84), 100, 32);
        newSong.Click += (_, _) => NewSong();
        var saveSong = Button("Save", _brand, Color.White, 62, 32);
        saveSong.Click += (_, _) => SaveCurrentSong();
        var deleteSong = Button("Delete", Color.White, Color.FromArgb(177, 59, 54), 66, 32);
        deleteSong.Click += (_, _) => DeleteCurrentSong();
        songActions.Controls.AddRange([newSong, saveSong, deleteSong]);
        content.Controls.Add(songActions, 0, 0);
        content.Controls.Add(SmallLabel("SONG TITLE"), 0, 1);
        _titleBox = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) };
        _titleBox.TextChanged += (_, _) => { if (_slideStatus is not null) _slideStatus.Text = "Editing · " + (string.IsNullOrWhiteSpace(_titleBox.Text) ? "Untitled song" : _titleBox.Text); };
        content.Controls.Add(_titleBox, 0, 2);
        _lyricsBox = new RichTextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12F), AcceptsTab = true, Margin = new Padding(0, 0, 0, 8) };
        _lyricsBox.TextChanged += (_, _) => { if (!_updating) RebuildSlides(); };
        content.Controls.Add(_lyricsBox, 0, 3);
        _slideStatus = new Label { ForeColor = Color.FromArgb(88, 103, 120), AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
        content.Controls.Add(_slideStatus, 0, 4);
        return outer;
    }

    private Control BuildPreviewPanel()
    {
        var outer = Section("Live slide", "What your congregation will see");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 4;
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _audiencePreview = new SlideCanvas { Dock = DockStyle.Fill, Theme = _theme, Margin = new Padding(0, 0, 0, 6), BackColor = Color.FromArgb(22, 28, 37) };
        var previewLabel = SmallLabel("PREVIEW");
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, WrapContents = false, Margin = new Padding(0) };
        var previous = Button("‹  Previous", Color.White, Color.FromArgb(52, 64, 84), 92, 33);
        previous.Click += (_, _) => SelectSlide(_currentSlide - 1);
        var live = Button("●  Go live", _brand, Color.White, 86, 33);
        live.Click += (_, _) => ToggleProjector();
        var next = Button("Next  ›", _brand, Color.White, 78, 33);
        next.Click += (_, _) => SelectSlide(_currentSlide + 1);
        buttons.Controls.AddRange([previous, live, next]);
        var help = new Label { Text = "Tip: use ↑ / ↓ to change slides", ForeColor = Color.FromArgb(112, 125, 138), AutoSize = true, Margin = new Padding(0, 7, 0, 0) };
        content.Controls.Add(_audiencePreview, 0, 0);
        content.Controls.Add(previewLabel, 0, 1);
        content.Controls.Add(buttons, 0, 2);
        content.Controls.Add(help, 0, 3);
        return outer;
    }

    private Panel Section(string title, string subtitle)
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10, 8, 10, 10), Margin = new Padding(0, 0, 6, 0) };
        outer.Paint += (_, e) =>
        {
            using var pen = new Pen(_panelBorder, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, outer.ClientSize.Width - 1, outer.ClientSize.Height - 1);
        };
        var header = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.White };
        header.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = Color.FromArgb(31, 41, 55), AutoSize = true, Location = new Point(0, 0) });
        header.Controls.Add(new Label { Text = subtitle, ForeColor = Color.FromArgb(102, 112, 133), AutoSize = true, Location = new Point(0, 22) });
        header.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(231, 234, 240)); e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1); };
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(0) };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outer.Controls.Add(content);
        outer.Controls.Add(header);
        outer.Tag = content;
        return outer;
    }

    private Panel RibbonGroup(string title, int width)
    {
        var group = new Panel { Width = width, Height = 122, Margin = new Padding(4, 4, 4, 0), BackColor = Color.White };
        group.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(228, 232, 237));
            e.Graphics.DrawLine(pen, group.Width - 1, 10, group.Width - 1, group.Height - 16);
        };
        var caption = new Label
        {
            Text = title,
            Dock = DockStyle.Bottom,
            Height = 16,
            TextAlign = ContentAlignment.BottomRight,
            ForeColor = Color.FromArgb(130, 140, 152),
            Font = new Font("Segoe UI", 7.5F),
            BackColor = Color.White,
            Padding = new Padding(0, 0, 8, 2)
        };
        var items = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = true,
            AutoSize = false,
            BackColor = Color.White,
            Padding = new Padding(8, 8, 8, 4)
        };
        group.Controls.Add(items);
        group.Controls.Add(caption);
        group.Tag = items;
        return group;
    }

    private RibbonIconButton RibbonButton(RibbonGlyph glyph, string caption, int width, int height)
        => new() { Glyph = glyph, Text = caption, Width = width, Height = height };

    private RibbonIconButton AlignButton(RibbonGlyph glyph, string tip)
    {
        var button = new RibbonIconButton { Glyph = glyph, Width = 28, Height = 40, Margin = new Padding(0, 0, 4, 0) };
        var tipService = new ToolTip();
        tipService.SetToolTip(button, tip);
        return button;
    }

    private Button ToggleStyleButton(string text, Font font, string tip)
    {
        var button = new Button
        {
            Text = text,
            Width = 28,
            Height = 28,
            Font = font,
            FlatStyle = FlatStyle.Flat,
            BackColor = RibbonIconButton.IdleBack,
            ForeColor = RibbonIconButton.IdleInk,
            FlatAppearance = { BorderSize = 1, BorderColor = RibbonIconButton.IdleBorder },
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Margin = new Padding(0, 0, 4, 0),
            Tag = false
        };
        var tipService = new ToolTip();
        tipService.SetToolTip(button, tip);
        button.MouseEnter += (_, _) => button.BackColor = button.Tag is true ? RibbonIconButton.ActiveBack : RibbonIconButton.HoverBack;
        button.MouseLeave += (_, _) => button.BackColor = button.Tag is true ? RibbonIconButton.ActiveBack : RibbonIconButton.IdleBack;
        return button;
    }

    private static Button RibbonActionButton(string text, Color background, Color foreground, int width, int height)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = height,
            BackColor = background,
            ForeColor = foreground,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Margin = new Padding(0, 0, 6, 0),
            Padding = new Padding(4, 0, 4, 0)
        };
        button.FlatAppearance.BorderColor = ControlPaint.Dark(background, 0.15F);
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(background, 0.12F);
        return button;
    }

    private static Control MiniField(string label, Control input)
    {
        var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
        panel.Controls.Add(new Label { Text = label, ForeColor = Color.FromArgb(86, 99, 114), Font = new Font("Segoe UI", 7F), AutoSize = true, Margin = new Padding(2, 2, 0, 2) });
        panel.Controls.Add(input);
        return panel;
    }

    private Button StyleButton(string label, Color background, Color foreground, Action action)
    {
        var btn = Button(label, background, foreground, 72, 48);
        btn.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => action();
        return btn;
    }



    private static void Add(Panel group, params Control[] controls) => ((FlowLayoutPanel)group.Tag!).Controls.AddRange(controls);

    private static Control Field(string label, Control input)
    {
        var panel = new FlowLayoutPanel { Width = 118, Height = 52, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 8, 0) };
        panel.Controls.Add(new Label { Text = label, ForeColor = Color.FromArgb(86, 99, 114), AutoSize = true });
        panel.Controls.Add(input);
        return panel;
    }

    private Button TabButton(string text, bool active)
    {
        var boldFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        var regularFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        var measured = TextRenderer.MeasureText(text, boldFont);
        var button = new Button
        {
            Text = text,
            Width = Math.Max(60, measured.Width + 24),
            Height = 32,
            Margin = new Padding(0, 0, 6, 0),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = Color.White,
            ForeColor = active ? _brand : Color.FromArgb(102, 112, 133),
            Font = active ? boldFont : regularFont,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Tag = active
        };
        button.Paint += (_, e) =>
        {
            if (button.Tag is true)
            {
                using var brush = new SolidBrush(_brand);
                e.Graphics.FillRectangle(brush, 0, button.Height - 2, button.Width, 2);
            }
        };
        button.MouseEnter += (_, _) => { if (button.Tag is not true) { button.BackColor = Color.FromArgb(243, 246, 255); button.ForeColor = Color.FromArgb(52, 64, 84); } };
        button.MouseLeave += (_, _) => { if (button.Tag is not true) { button.BackColor = Color.White; button.ForeColor = Color.FromArgb(102, 112, 133); } };
        return button;
    }

    private void SetTabState(Button button, bool active)
    {
        button.Tag = active;
        button.BackColor = Color.White;
        button.ForeColor = active ? _brand : Color.FromArgb(102, 112, 133);
        var previous = button.Font;
        button.Font = new Font("Segoe UI", 9.5F, active ? FontStyle.Bold : FontStyle.Regular);
        previous?.Dispose();
        button.Invalidate();
    }

    private static Button Button(string text, Color background, Color foreground, int width, int height) => new()
    {
        Text = text, Width = width, Height = height, BackColor = background, ForeColor = foreground, FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(0, 0, 6, 0), Cursor = Cursors.Hand,
        FlatAppearance = { BorderColor = Color.FromArgb(218, 224, 231), BorderSize = 1 }, UseVisualStyleBackColor = false
    };

    private Button Preset(string text, Color background, Color foreground, Action action)
    {
        var button = Button(text, background, foreground, 79, 47);
        button.Click += (_, _) => action();
        return button;
    }

    private static Label Hint(string text) => new() { Text = text, ForeColor = Color.FromArgb(115, 125, 138), AutoSize = true, MaximumSize = new Size(210, 0), Margin = new Padding(8, 6, 0, 0) };
    private static Label SmallLabel(string text) => new() { Text = text, ForeColor = Color.FromArgb(107, 118, 131), Font = new Font("Segoe UI", 8F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
}
