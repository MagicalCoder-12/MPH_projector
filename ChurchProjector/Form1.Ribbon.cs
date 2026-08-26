namespace ChurchProjector;

public partial class Form1
{
    private void BuildInterface()
    {
        SuspendLayout();
        Text = "MPH Songs";
        MinimumSize = new Size(1120, 720);
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(241, 244, 247);
        Font = new Font("Segoe UI", 9F);

        var header = BuildHeader();
        var tabBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = _darkBrand, Padding = new Padding(10, 5, 0, 5) };
        var backgroundTab = TabButton("▧  Background", false);
        backgroundTab.Dock = DockStyle.Left;
        var bibleTab = TabButton("Bible", false);
        bibleTab.Dock = DockStyle.Left;
        var helpTab = TabButton("Help", false);
        helpTab.Dock = DockStyle.Left;
        var textTab = TabButton("A  Text", true);
        textTab.Dock = DockStyle.Left;
        tabBar.Controls.Add(helpTab);
        tabBar.Controls.Add(bibleTab);
        tabBar.Controls.Add(backgroundTab);
        tabBar.Controls.Add(textTab);

        var ribbonHost = new Panel { Dock = DockStyle.Top, Height = 172, BackColor = Color.White, Padding = new Padding(10, 8, 10, 8) };
        var textRibbon = BuildTextRibbon();
        var backgroundRibbon = BuildBackgroundRibbon();
        var bibleRibbon = BuildBibleRibbon();
        var helpRibbon = BuildHelpRibbon();
        backgroundRibbon.Visible = false;
        bibleRibbon.Visible = false;
        helpRibbon.Visible = false;
        ribbonHost.Controls.Add(textRibbon);
        ribbonHost.Controls.Add(backgroundRibbon);
        ribbonHost.Controls.Add(bibleRibbon);
        ribbonHost.Controls.Add(helpRibbon);
        _songWorkspace = BuildWorkspace();
        _bibleWorkspace = BuildBibleWorkspace();
        _helpWorkspace = BuildHelpWorkspace();
        _bibleWorkspace.Visible = false;
        _helpWorkspace.Visible = false;
        var workspaceHost = new Panel { Dock = DockStyle.Fill };
        workspaceHost.Controls.Add(_songWorkspace);
        workspaceHost.Controls.Add(_bibleWorkspace);
        workspaceHost.Controls.Add(_helpWorkspace);
        _activeAgendaList = _agendaList;
        textTab.Click += (_, _) => { RebuildSlides(); SwitchMode(textRibbon, _songWorkspace, textTab, backgroundTab, bibleTab, helpTab); };
        backgroundTab.Click += (_, _) => { RebuildSlides(); SwitchMode(backgroundRibbon, _songWorkspace, backgroundTab, textTab, bibleTab, helpTab); };
        bibleTab.Click += (_, _) => SwitchMode(bibleRibbon, _bibleWorkspace, bibleTab, textTab, backgroundTab, helpTab);
        helpTab.Click += (_, _) => SwitchMode(helpRibbon, _helpWorkspace, helpTab, textTab, backgroundTab, bibleTab);

        var statusBar = BuildStatusBar();
        Controls.Add(workspaceHost);
        Controls.Add(ribbonHost);
        Controls.Add(tabBar);
        Controls.Add(header);
        Controls.Add(statusBar);
        ResumeLayout();
    }

    private Control BuildStatusBar()
    {
        _statusBar = new StatusStrip
        {
            Dock = DockStyle.Bottom,
            BackColor = _darkBrand,
            ForeColor = Color.White,
            Padding = new Padding(2, 1, 2, 1),
            ShowItemToolTips = true
        };
        var border = ToolStripStatusLabelBorderSides.Right;
        _statusTab = new ToolStripStatusLabel("Text") { ForeColor = Color.White, BorderSides = border };
        _statusSlide = new ToolStripStatusLabel("Slide 1 of 1") { ForeColor = Color.White, BorderSides = border };
        _statusProjector = new ToolStripStatusLabel("Projector: off") { ForeColor = Color.White, BorderSides = border };
        _statusClock = new ToolStripStatusLabel(DateTime.Now.ToShortTimeString()) { ForeColor = Color.White, Spring = true, TextAlign = ContentAlignment.MiddleRight };
        _statusSync = new ToolStripStatusLabel(_data.Sync.UseMongoDb ? "\u25cb Connecting..." : "\u25cb Local mode") { ForeColor = _data.Sync.UseMongoDb ? Color.FromArgb(255, 220, 120) : Color.FromArgb(190, 190, 190), BorderSides = border };
        _statusBar.Items.AddRange([_statusTab, _statusSlide, _statusProjector, _statusSync, _statusClock]);
        return _statusBar;
    }

    private void SwitchMode(Control ribbon, Control workspace, Button active, params Button[] inactive)
    {
        foreach (Control control in ribbon.Parent!.Controls) control.Visible = control == ribbon;
        foreach (Control control in workspace.Parent!.Controls) control.Visible = control == workspace;
        active.BackColor = Color.White;
        active.ForeColor = _darkBrand;
        foreach (var button in inactive)
        {
            button.BackColor = _darkBrand;
            button.ForeColor = Color.White;
        }
        if (_statusTab is not null)
            _statusTab.Text = "Tab: " + active.Text.Replace("▧", "").Replace("A", "").Trim();
        _currentTab = active.Text.Contains("Bible") ? "bible" : active.Text.Contains("Background") ? "background" : active.Text.Contains("Help") ? "help" : "text";
        _activeAgendaList = _currentTab == "bible" ? _bibleAgendaList : _agendaList;
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = _brand };
        var brand = new TableLayoutPanel { Dock = DockStyle.Left, AutoSize = true, Height = 48, ColumnCount = 3, RowCount = 1, BackColor = _brand, Padding = new Padding(14, 0, 0, 0) };
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        brand.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        var star = new Label { Text = "✦", Font = new Font("Segoe UI Symbol", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(255, 210, 64), AutoSize = true, Anchor = AnchorStyles.None };
        var title = new Label { Text = "MPH SONGS", ForeColor = Color.White, Font = new Font("Segoe UI", 13F, FontStyle.Bold), AutoSize = true, Anchor = AnchorStyles.None, Margin = new Padding(10, 0, 14, 0) };
        _headerSubtitle = new Label { Text = "Sunday service · Ready", ForeColor = Color.FromArgb(213, 235, 251), AutoSize = true, Anchor = AnchorStyles.None, Margin = new Padding(0, 2, 0, 0) };
        brand.Controls.Add(star, 0, 0);
        brand.Controls.Add(title, 1, 0);
        brand.Controls.Add(_headerSubtitle, 2, 0);
        header.Controls.Add(brand);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Height = 48, Padding = new Padding(0, 8, 14, 0), WrapContents = false, BackColor = _brand };
        _blackButton = Button("◼  Black", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 84, 31);
        _blackButton.Click += (_, _) => SetStageMode(StageMode.Black);
        Tip(_blackButton, "Show a black screen between songs (F2)");
        _hideTextButton = Button("▦  Hide text", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 104, 31);
        _hideTextButton.Click += (_, _) => SetStageMode(StageMode.Background);
        Tip(_hideTextButton, "Keep the background but hide the words (F3)");
        _logoButton = Button("✦  Logo", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 84, 31);
        _logoButton.Click += (_, _) => SetStageMode(StageMode.Logo);
        Tip(_logoButton, "Show the church logo (F4)");
        _projectorButton = Button("▣  Open projector", Color.FromArgb(11, 77, 132), Color.White, 162, 31);
        _projectorButton.Click += (_, _) => ToggleProjector();
        Tip(_projectorButton, "Open or close the borderless projector window (F5)");
        actions.Controls.AddRange([_blackButton, _hideTextButton, _logoButton, _projectorButton]);
        header.Controls.Add(actions);
        return header;
    }

    private Control BuildTextRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        EnableHorizontalWheel(ribbon);

        // Clipboard group
        var clipboard = RibbonGroup("Clipboard", 205);
        _cutButton = new RibbonButton("✂", "Cut");
        _cutButton.Click += (_, _) => _lyricsBox.Cut();
        Tip(_cutButton, "Cut the selected text (Ctrl+X)");
        _copyButton = new RibbonButton("▣", "Copy");
        _copyButton.Click += (_, _) => _lyricsBox.Copy();
        Tip(_copyButton, "Copy the selected text (Ctrl+C)");
        _pasteButton = new RibbonButton("▤", "Paste");
        _pasteButton.Click += (_, _) => _lyricsBox.Paste();
        Tip(_pasteButton, "Paste from the clipboard (Ctrl+V)");
        Add(clipboard, _cutButton, _copyButton, _pasteButton);

        // Font group
        var font = RibbonGroup("Font", 850);
        _fontFamily = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 168, DropDownWidth = 280 };
        var fonts = new List<string> { "Segoe UI", "Arial", "Calibri", "Cambria", "Georgia", "Verdana", "Trebuchet MS", "Times New Roman", "Tahoma", "Century Gothic" };
        var telugu = new[] { "Nirmala UI", "Noto Sans Telugu", "Gautami", "Vani", "Lohit Telugu", "Telugu Sangam MN", "Raghu Telugu", "Kalinga", "Shruti", "Tunga", "Malgun Gothic", "Microsoft Himalaya" };
        var installed = new HashSet<string>(FontFamily.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        fonts.AddRange(telugu.Where(installed.Contains));
        _fontFamily.Items.AddRange([.. fonts.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f)]);
        var selectedFont = _fontFamily.Items.Cast<string>().FirstOrDefault(f => string.Equals(f, _theme.FontFamily, StringComparison.OrdinalIgnoreCase));
        _fontFamily.SelectedItem = selectedFont ?? _fontFamily.Items[0];
        Tip(_fontFamily, "Choose the font for the projected lyrics");
        _fontFamily.SelectedIndexChanged += (_, _) => { _theme.FontFamily = _fontFamily.Text; RefreshSlides(); };

        _fontSize = new NumericUpDown { Minimum = 8, Maximum = 200, Value = (int)_theme.FontSize, Width = 56 };
        Tip(_fontSize, "Base font size of the lyrics");
        _fontSize.ValueChanged += (_, _) => { _theme.FontSize = (float)_fontSize.Value; RefreshSlides(); };

        _autoFit = new CheckBox { Text = "Auto-fit", Checked = _theme.AutoFit, AutoSize = true, Margin = new Padding(8, 16, 4, 0) };
        Tip(_autoFit, "Automatically shrink or grow the lyrics so they fill the screen");
        _autoFit.CheckedChanged += (_, _) => { _theme.AutoFit = _autoFit.Checked; SaveBackgroundPreferences(); RefreshSlides(); };

        _boldButton = new RibbonButton("B", "Bold");
        _boldButton.Click += (_, _) => { _theme.Bold = !_theme.Bold; UpdateBoldButton(); RefreshSlides(); };
        Tip(_boldButton, "Bold lyrics");
        _italicButton = new RibbonButton("I", "Italic");
        _italicButton.Click += (_, _) => { _theme.Italic = !_theme.Italic; UpdateItalicButton(); RefreshSlides(); };
        Tip(_italicButton, "Italic lyrics");
        _underlineButton = new RibbonButton("U", "Underline");
        _underlineButton.Click += (_, _) => { _theme.Underline = !_theme.Underline; UpdateUnderlineButton(); RefreshSlides(); };
        Tip(_underlineButton, "Underline lyrics");
        _strikethroughButton = new RibbonButton("ABC", "Strike");
        _strikethroughButton.Click += (_, _) => { _theme.Strikethrough = !_theme.Strikethrough; UpdateStrikethroughButton(); RefreshSlides(); };
        Tip(_strikethroughButton, "Strikethrough lyrics");
        _subscriptButton = new RibbonButton("X₂", "Subscript");
        _subscriptButton.Click += (_, _) => { _theme.Subscript = !_theme.Subscript; _theme.Superscript = false; UpdateSubSuperButtons(); RefreshSlides(); };
        Tip(_subscriptButton, "Subscript lyrics");
        _superscriptButton = new RibbonButton("X²", "Superscript");
        _superscriptButton.Click += (_, _) => { _theme.Superscript = !_theme.Superscript; _theme.Subscript = false; UpdateSubSuperButtons(); RefreshSlides(); };
        Tip(_superscriptButton, "Superscript lyrics");
        _fontColorButton = new RibbonButton("A", "Colour");
        _fontColorButton.Click += (_, _) => ChooseTextColor();
        Tip(_fontColorButton, "Choose the text colour");
        _clearFormattingButton = new RibbonButton("✕", "Clear");
        _clearFormattingButton.Click += (_, _) => ClearFormatting();
        Tip(_clearFormattingButton, "Reset all text formatting to the default");

        Add(font, Field("Font family", _fontFamily, 178), Field("Size", _fontSize, 66), _autoFit,
            _boldButton, _italicButton, _underlineButton, _strikethroughButton, _subscriptButton, _superscriptButton,
            _fontColorButton, _clearFormattingButton);
        UpdateFontColourButton();

        // Paragraph group
        var paragraph = RibbonGroup("Paragraph", 435);
        _alignment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        _alignment.Items.AddRange(["Left", "Centre", "Right", "Justify"]);
        _alignment.SelectedItem = "Centre";
        Tip(_alignment, "Align the lyrics on the slide");
        _alignment.SelectedIndexChanged += (_, _) => { _theme.Alignment = _alignment.Text; RefreshSlides(); };

        _bulletsButton = new RibbonButton("•", "Bullets");
        _bulletsButton.Click += (_, _) => ToggleBullets();
        Tip(_bulletsButton, "Add or remove bullets on the current lines");
        _numberingButton = new RibbonButton("1.", "Numbers");
        _numberingButton.Click += (_, _) => ToggleNumbering();
        Tip(_numberingButton, "Add or remove numbering on the current lines");

        _lineSpacing = new NumericUpDown { Minimum = 1, Maximum = 5, Value = 1, Increment = 0.1m, Width = 48 };
        Tip(_lineSpacing, "Line spacing of the lyrics (1.0 = normal)");
        _lineSpacing.ValueChanged += (_, _) => { _theme.LineSpacing = (float)_lineSpacing.Value; RefreshSlides(); };

        _maxLines = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 4, Width = 48 };
        Tip(_maxLines, "Maximum lyric lines per slide");
        _maxLines.ValueChanged += (_, _) => RebuildSlides();

        Add(paragraph, Field("Align", _alignment, 112), _bulletsButton, _numberingButton,
            Field("Spacing", _lineSpacing, 70), Field("Max lines", _maxLines, 82));

        // Styles group
        var styles = RibbonGroup("Styles", 340);
        var lightPreview = PreviewStyle("Light", Color.White, Color.FromArgb(28, 34, 45), () => SetTextStyle(Color.White, true));
        Tip(lightPreview, "Light — white text on a dark background");
        var warmPreview = PreviewStyle("Warm", Color.FromArgb(255, 239, 171), Color.FromArgb(71, 48, 40), () => SetTextStyle(Color.FromArgb(255, 239, 171), true));
        Tip(warmPreview, "Warm — cream text with a warm tone");
        var darkPreview = PreviewStyle("Dark", Color.FromArgb(28, 40, 52), Color.FromArgb(232, 240, 245), () => SetTextStyle(Color.FromArgb(28, 40, 52), false));
        Tip(darkPreview, "Dark — dark text on a light background");
        var titlePreview = PreviewStyle("Title", Color.FromArgb(255, 210, 64), Color.FromArgb(22, 34, 52), () => SetTextStyle(Color.FromArgb(255, 210, 64), true));
        Tip(titlePreview, "Title — golden accent colour");
        Add(styles, lightPreview, warmPreview, darkPreview, titlePreview);

        // Cloud sync group
        var sync = RibbonGroup("Cloud sync", 300);
        var syncNow = Button("\u21bb  Sync now", _brand, Color.White, 108, 36);
        Tip(syncNow, "Pull songs from the web app and push local songs to MongoDB Atlas");
        syncNow.Click += async (_, _) =>
        {
            syncNow.Enabled = false;
            syncNow.Text = "\u21bb  Syncing...";
            try
            {
                await SyncFromMongoAsync();
                _slideStatus.Text = _lastSyncFailed ? $"Sync failed: {_syncError ?? "check connection"}" : "Synced with cloud";
            }
            finally
            {
                syncNow.Text = "\u21bb  Sync now";
                syncNow.Enabled = true;
            }
        };
        Add(sync, syncNow, Hint(_data.Sync.UseMongoDb ? "Pull web songs and push local songs to MongoDB Atlas." : "Set a MongoDB URI on the Help tab to enable cloud sync."));

        ribbon.Controls.AddRange([clipboard, font, paragraph, styles, sync]);
        return ribbon;
    }

    private Control BuildBackgroundRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        EnableHorizontalWheel(ribbon);

        var colour = RibbonGroup("Colour", 320, 148);
        var colours = new (Color Value, string Name)[]
        {
            (Color.FromArgb(22, 34, 52), "Midnight"),
            (Color.FromArgb(12, 79, 105), "Ocean"),
            (Color.FromArgb(74, 33, 95), "Royal"),
            (Color.FromArgb(104, 55, 36), "Ember"),
            (Color.FromArgb(49, 80, 58), "Forest"),
            (Color.FromArgb(238, 240, 243), "Light")
        };
        foreach (var preset in colours)
        {
            var swatch = Button("", preset.Value, Color.White, 40, 40);
            swatch.FlatAppearance.BorderSize = 0;
            swatch.Click += (_, _) => SetSolidBackground(preset.Value);
            Tip(swatch, "Solid " + preset.Name + " background");
            Add(colour, swatch);
        }
        var more = Button("More…", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 62, 40);
        more.Click += (_, _) => ChooseBackgroundColor();
        Tip(more, "Choose any background colour");
        Add(colour, more);

        var media = RibbonGroup("Saved backgrounds", 500, 148);
        var importRow = new FlowLayoutPanel { Width = 452, Height = 36, WrapContents = false, Margin = new Padding(0) };
        var choose = Button("Import image", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 96, 34);
        choose.Click += (_, _) => ChooseBackgroundImage();
        Tip(choose, "Copy an image into the background library");
        var video = Button("Import video", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 96, 34);
        video.Click += (_, _) => ImportBackground("Video");
        Tip(video, "Copy a video into the background library");
        var logo = Button("Set logo", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 80, 34);
        logo.Click += (_, _) => SetLogoPath();
        Tip(logo, "Choose the image used by the Logo stage button");
        var delete = Button("Delete", Color.White, Color.FromArgb(177, 59, 54), 72, 34);
        delete.Click += (_, _) => DeleteBackgroundAsset();
        Tip(delete, "Remove the selected background from the library");
        importRow.Controls.AddRange([choose, video, logo, delete]);

        _backgroundList = new ListView
        {
            Width = 250,
            Height = 54,
            View = View.LargeIcon,
            BorderStyle = BorderStyle.FixedSingle,
            MultiSelect = false,
            HideSelection = false,
            Margin = new Padding(0, 4, 8, 0)
        };
        _backgroundList.SelectedIndexChanged += (_, _) =>
        {
            if (_updating || _backgroundList.SelectedItems.Count == 0) return;
            if (_backgroundList.SelectedItems[0].Tag is BackgroundAsset asset) ApplyBackgroundAsset(asset);
        };

        _videoLoop = new CheckBox { Text = "Loop video", Checked = _theme.VideoLoop, AutoSize = true, Margin = new Padding(0, 18, 8, 0) };
        _videoLoop.CheckedChanged += (_, _) => { _theme.VideoLoop = _videoLoop.Checked; SaveBackgroundPreferences(); RefreshSlides(); };
        Tip(_videoLoop, "Repeat the background video, or play it once and hold the last frame");
        var clear = Button("Clear", Color.White, Color.FromArgb(31, 48, 68), 60, 32);
        clear.Click += (_, _) => { ClearBackgroundSelection(); SaveBackgroundPreferences(); RefreshSlides(); };
        Tip(clear, "Return to the solid background colour");
        Add(media, importRow, _backgroundList, _videoLoop, clear);
        RefreshBackgroundPicker();

        var brightness = RibbonGroup("Brightness", 220, 148);
        _brightness = new TrackBar { Minimum = -75, Maximum = 75, Value = _theme.Brightness, TickFrequency = 25, Width = 150, Height = 38 };
        _brightness.ValueChanged += (_, _) => { _theme.Brightness = _brightness.Value; SaveBackgroundPreferences(); RefreshSlides(); };
        Tip(_brightness, "Darken or brighten the background so lyrics stay readable");
        var brightnessValue = new Label
        {
            Text = _brightness.Value >= 0 ? $"+{_brightness.Value}" : _brightness.Value.ToString(),
            ForeColor = Color.FromArgb(86, 99, 114),
            AutoSize = true,
            Margin = new Padding(6, 14, 0, 0)
        };
        _brightness.ValueChanged += (_, _) => brightnessValue.Text = _brightness.Value >= 0 ? $"+{_brightness.Value}" : _brightness.Value.ToString();
        Add(brightness, _brightness, brightnessValue);

        var ratio = RibbonGroup("Aspect ratio", 372, 148);
        foreach (var value in new[] { "16:9", "4:3", "16:10", "9:16", "3:4", "Current" })
        {
            var choice = Button(value, value == _theme.AspectRatio ? _brand : Color.White, value == _theme.AspectRatio ? Color.White : Color.FromArgb(31, 48, 68), value.Length > 5 ? 64 : 52, 32);
            choice.Click += (_, _) => SetAspectRatio(value, ratio);
            Tip(choice, value == "Current" ? "Fill the whole projector screen, ignoring aspect ratio" : "Fit the slide to " + value);
            Add(ratio, choice);
        }

        ribbon.Controls.AddRange([colour, media, brightness, ratio]);
        return ribbon;
    }

    private void SetAspectRatio(string value, Panel group)
    {
        _theme.AspectRatio = value;
        foreach (Control control in ((FlowLayoutPanel)group.Tag!).Controls.OfType<Button>())
        {
            var selected = control.Text == value;
            control.BackColor = selected ? _brand : Color.White;
            control.ForeColor = selected ? Color.White : Color.FromArgb(31, 48, 68);
        }
        SaveBackgroundPreferences();
        RefreshSlides();
    }
}
