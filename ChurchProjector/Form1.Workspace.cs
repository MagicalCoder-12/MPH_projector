namespace ChurchProjector;

public partial class Form1
{
    private Control BuildWorkspace()
    {
        var outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 10, 10, 12), BackColor = Color.FromArgb(241, 244, 247) };

        // Left: library  | Right: editor (middle) | verses + preview (right)
        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,   
            BackColor = Color.FromArgb(241, 244, 247),
            SplitterWidth = 6
        };

        // Left panel: Song Library
        var libraryOuter = Section("Song library", "Your saved songs");
        var libraryContent = (TableLayoutPanel)libraryOuter.Tag!;

        // Two rows:
        // Row 0 = Search box
        // Row 1 = Song list
        libraryContent.RowCount = 2;
        libraryContent.RowStyles.Clear();
        libraryContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Search
        libraryContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Song list fills remaining space

        // Search box
        _librarySearch = new TextBox
        {
            PlaceholderText = "Search songs",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6)
        };

        _librarySearch.TextChanged += (_, _) => FilterLibrary(_librarySearch.Text);
        libraryContent.Controls.Add(_librarySearch, 0, 0);

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

        libraryContent.Controls.Add(_libraryList, 0, 1);

        // Populate the library immediately so songs are visible on startup
        FilterLibrary("");

        // Add the library section to the left panel
        mainSplit.Panel1.Controls.Add(libraryOuter);


        var rightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = Color.FromArgb(241, 244, 247),
            SplitterWidth = 6
        };

        // Middle column
        rightSplit.Panel1.Controls.Add(BuildEditorPanel());

        // Right column (Verses + Preview)
        var previewSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BackColor = Color.FromArgb(241, 244, 247),
            SplitterWidth = 6
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
        var outer = Section("Slides / Verses", "Select a slide to preview");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 2;
        content.RowStyles.Clear();
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        content.Controls.Add(SmallLabel("SLIDES"), 0, 0);
        _slideList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0) };
        _slideList.SelectedIndexChanged += (_, _) => { if (!_updating && _slideList.SelectedIndex >= 0) SelectSlide(_slideList.SelectedIndex); };
        content.Controls.Add(_slideList, 0, 1);
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
        var remove = Button("Remove", Color.White, Color.FromArgb(31, 48, 68), 56, 32);
        remove.Click += (_, _) => RemoveAgendaItem();
        var update = Button("Update", Color.White, Color.FromArgb(31, 48, 68), 55, 32);
        update.Click += (_, _) => UpdateAgendaItem();
        var up = Button("Up", Color.White, Color.FromArgb(31, 48, 68), 28, 32);
        up.Click += (_, _) => MoveAgendaItem(-1);
        var down = Button("Down", Color.White, Color.FromArgb(31, 48, 68), 36, 32);
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
        var newSong = Button("New song", Color.White, Color.FromArgb(31, 48, 68), 100, 32);
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
        _audiencePreview = new SlideCanvas { Dock = DockStyle.Fill, Theme = _theme, Margin = new Padding(0, 0, 0, 9), BackColor = Color.FromArgb(22, 28, 37) };
        var previewLabel = SmallLabel("PREVIEW");
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, WrapContents = false, Margin = new Padding(0) };
        var previous = Button("‹  Previous", Color.White, Color.FromArgb(31, 48, 68), 92, 33);
        previous.Click += (_, _) => SelectSlide(_currentSlide - 1);
        var live = Button("●  Go live", Color.FromArgb(35, 157, 87), Color.White, 86, 33);
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
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12), Margin = new Padding(0, 0, 10, 0) };
        outer.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, outer.ClientRectangle, _panelBorder, ButtonBorderStyle.Solid);
        var header = new Panel { Dock = DockStyle.Top, Height = 47, BackColor = Color.White };
        header.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(31, 48, 68), AutoSize = true, Location = new Point(0, 0) });
        header.Controls.Add(new Label { Text = subtitle, ForeColor = Color.FromArgb(112, 125, 138), AutoSize = true, Location = new Point(0, 25) });
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(0) };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outer.Controls.Add(content);
        outer.Controls.Add(header);
        outer.Tag = content;
        return outer;
    }

    private Panel RibbonGroup(string title, int width)
    {
        var group = new Panel { Width = width, Height = 130, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(9, 28, 9, 22), BackColor = Color.White };
        group.Paint += (_, e) => { using var pen = new Pen(_panelBorder); e.Graphics.DrawLine(pen, group.Width - 1, 28, group.Width - 1, group.Height - 4); };
        var label = new Label { Text = title.ToUpperInvariant(), ForeColor = Color.FromArgb(77, 93, 111), Font = new Font("Segoe UI", 9F, FontStyle.Bold), AutoSize = true, Location = new Point(10, 6) };
        var items = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoSize = false, BackColor = Color.White, Padding = new Padding(6, 6, 6, 6) };
        group.Controls.Add(items);
        group.Controls.Add(label);
        group.Tag = items;
        return group;
    }

    private Button StyleButton(string label, Color background, Color foreground, Action action)
    {
        var btn = Button(label, background, foreground, 72, 48);
        btn.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => action();
        return btn;
    }

    private Control PreviewStyle(string name, Color background, Color foreground, Action action)
    {
        var panel = new Panel { Width = 72, Height = 48, BackColor = Color.White, Margin = new Padding(4), Cursor = Cursors.Hand };
        var preview = new Panel { Width = 60, Height = 30, BackColor = background, Location = new Point(6, 8) };
        var sample = new Label { Text = "Aa", ForeColor = foreground, Font = new Font("Segoe UI", 10F, FontStyle.Bold), AutoSize = true, Location = new Point((preview.Width - 18) / 2, (preview.Height - 16) / 2) };
        preview.Controls.Add(sample);
        panel.Controls.Add(preview);
        var caption = new Label { Text = name, ForeColor = Color.FromArgb(77, 93, 111), Font = new Font("Segoe UI", 8F), AutoSize = true, Location = new Point(6, 38) };
        panel.Controls.Add(caption);
        panel.Click += (_, _) => action();
        preview.Click += (_, _) => action();
        sample.Click += (_, _) => action();
        caption.Click += (_, _) => action();
        return panel;
    }

    private static void Add(Panel group, params Control[] controls) => ((FlowLayoutPanel)group.Tag!).Controls.AddRange(controls);

    private static Control Field(string label, Control input)
    {
        var panel = new FlowLayoutPanel { Width = 118, Height = 52, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 8, 0) };
        panel.Controls.Add(new Label { Text = label, ForeColor = Color.FromArgb(86, 99, 114), AutoSize = true });
        panel.Controls.Add(input);
        return panel;
    }

    private Button TabButton(string text, bool active) => new()
    {
        Text = text, Width = 112, FlatStyle = FlatStyle.Flat, BackColor = active ? Color.White : _darkBrand,
        ForeColor = active ? _darkBrand : Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand,
        FlatAppearance = { BorderSize = 0 }
    };

    private static Button Button(string text, Color background, Color foreground, int width, int height) => new()
    {
        Text = text, Width = width, Height = height, BackColor = background, ForeColor = foreground, FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(0, 0, 6, 0), Cursor = Cursors.Hand,
        FlatAppearance = { BorderColor = Color.FromArgb(196, 206, 216), BorderSize = 1 }, UseVisualStyleBackColor = false
    };

    private Button Preset(string text, Color background, Color foreground, Action action)
    {
        var button = Button(text, background, foreground, 79, 47);
        button.Click += (_, _) => action();
        return button;
    }

    private static Label Hint(string text) => new() { Text = text, ForeColor = Color.FromArgb(112, 125, 138), AutoSize = true, MaximumSize = new Size(210, 0), Margin = new Padding(8, 7, 0, 0) };
    private static Label SmallLabel(string text) => new() { Text = text, ForeColor = Color.FromArgb(77, 93, 111), Font = new Font("Segoe UI", 8F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
}
