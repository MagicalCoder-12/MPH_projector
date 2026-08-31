namespace ChurchProjector;

public partial class Form1
{
    private RibbonBar? _ribbonBar;

    private void BuildInterface()
    {
        SuspendLayout();
        Text = "MPH Songs";
        MinimumSize = new Size(1120, 720);
        WindowState = FormWindowState.Maximized;
        BackColor = AppTheme.AppBackground;
        Font = new Font("Segoe UI", 9F);

        // ── Application Header ──────────────────────────────────
        var header = BuildHeader();

        // ── Ribbon Bar (tabs + content) ─────────────────────────
        _ribbonBar = new RibbonBar { Dock = DockStyle.Top };
        _ribbonBar.TabChanged += (_, tabName) =>
        {
            if (_statusTab is not null) _statusTab.Text = tabName;
            _currentTab = tabName.Contains("Bible") ? "bible"
                : tabName.Contains("Background") ? "background"
                : tabName.Contains("Help") ? "help" : "text";
            _activeAgendaList = _currentTab == "bible" ? _bibleAgendaList : null;

            // Show/hide workspace
            if (_currentTab == "bible") { _bibleWorkspace.Visible = true; _songWorkspace.Visible = false; _helpWorkspace.Visible = false; }
            else if (_currentTab == "help") { _helpWorkspace.Visible = true; _songWorkspace.Visible = false; _bibleWorkspace.Visible = false; }
            else { _songWorkspace.Visible = true; _bibleWorkspace.Visible = false; _helpWorkspace.Visible = false; }
        };

        // Add in reverse order — Dock.Left places the last-added control leftmost
        var helpPage = _ribbonBar.AddTab("Help");
        var biblePage = _ribbonBar.AddTab("Bible");
        var backgroundPage = _ribbonBar.AddTab("Background");
        var textPage = _ribbonBar.AddTab("Text", active: true);

        BuildTextRibbon(textPage);
        BuildBackgroundRibbon(backgroundPage);
        BuildBibleRibbon(biblePage);
        BuildHelpRibbon(helpPage);

        _ribbonHost = _ribbonBar;

        // ── Workspace ───────────────────────────────────────────
        _songWorkspace = BuildWorkspace();
        _bibleWorkspace = BuildBibleWorkspace();
        _helpWorkspace = BuildHelpWorkspace();
        _bibleWorkspace.Visible = false;
        _helpWorkspace.Visible = false;

        var workspaceHost = new Panel { Dock = DockStyle.Fill };
        workspaceHost.Controls.Add(_songWorkspace);
        workspaceHost.Controls.Add(_bibleWorkspace);
        workspaceHost.Controls.Add(_helpWorkspace);

        var statusBar = BuildStatusBar();

        Controls.Add(workspaceHost);
        Controls.Add(_ribbonHost);
        Controls.Add(header);
        Controls.Add(statusBar);
        ResumeLayout();
    }

    // ═══════════════════════════════════════════════════════════════
    //  STATUS BAR
    // ═══════════════════════════════════════════════════════════════

    private Control BuildStatusBar()
    {
        _statusBar = new StatusStrip
        {
            Dock = DockStyle.Bottom,
            BackColor = AppTheme.StatusBarBg,
            ForeColor = AppTheme.StatusBarText,
            Padding = new Padding(2, 1, 2, 1),
            ShowItemToolTips = false,
            SizingGrip = false
        };
        var border = ToolStripStatusLabelBorderSides.Right;
        _statusTab = new ToolStripStatusLabel("Text") { ForeColor = AppTheme.StatusBarText, BorderSides = border };
        _statusSlide = new ToolStripStatusLabel("Slide 1 of 1") { ForeColor = AppTheme.StatusBarText, BorderSides = border };
        _statusProjector = new ToolStripStatusLabel("Projector: off") { ForeColor = AppTheme.StatusBarText, BorderSides = border };
        _statusSync = new ToolStripStatusLabel("\u25cb Connecting...") { ForeColor = Color.FromArgb(255, 220, 120), BorderSides = border };
        _statusClock = new ToolStripStatusLabel(DateTime.Now.ToShortTimeString()) { ForeColor = AppTheme.StatusBarText, Spring = true, TextAlign = ContentAlignment.MiddleRight };
        _statusBar.Items.AddRange([_statusTab, _statusSlide, _statusProjector, _statusSync, _statusClock]);
        return _statusBar;
    }

    // ═══════════════════════════════════════════════════════════════
    //  APPLICATION HEADER
    // ═══════════════════════════════════════════════════════════════

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = AppTheme.HeaderHeight, BackColor = AppTheme.HeaderBg };

        // Brand area (left)
        var brandPanel = new Panel { Dock = DockStyle.Left, Width = 320, BackColor = AppTheme.HeaderBg, Padding = new Padding(12, 0, 0, 0) };
        var starLabel = new Label
        {
            Text = "\u2726",
            Font = new Font("Segoe UI Symbol", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 210, 64),
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(0, 0, 6, 0)
        };
        var titleLabel = new Label
        {
            Text = "MPH SONGS",
            Font = AppTheme.GetFont(12F, FontStyle.Bold),
            ForeColor = AppTheme.HeaderText,
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 10, 0)
        };
        var subtitleLabel = new Label
        {
            Text = "Sunday service \u00b7 Ready",
            Font = AppTheme.GetFont(8.5F),
            ForeColor = AppTheme.HeaderSubtext,
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };
        brandPanel.Controls.Add(subtitleLabel);
        brandPanel.Controls.Add(titleLabel);
        brandPanel.Controls.Add(starLabel);
        header.Controls.Add(brandPanel);

        // Action buttons (right)
        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Height = AppTheme.HeaderHeight,
            Padding = new Padding(0, 5, 12, 0),
            WrapContents = false,
            BackColor = AppTheme.HeaderBg
        };

        _projectorButton = new Button
        {
            Text = "\u25a3  Open projector",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.AccentDarker,
            ForeColor = AppTheme.TextOnAccent,
            Font = AppTheme.GetFont(8.5F, FontStyle.Bold),
            Height = 28,
            Width = 155,
            Cursor = Cursors.Hand,
            Margin = new Padding(6, 0, 0, 0),
            UseVisualStyleBackColor = false
        };
        _projectorButton.FlatAppearance.BorderColor = AppTheme.AccentDarker;
        _projectorButton.FlatAppearance.MouseOverBackColor = AppTheme.AccentHover;
        _projectorButton.Click += (_, _) => ToggleProjector();

        _logoButton = new Button
        {
            Text = "\u2726  Logo",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.AccentLighter,
            ForeColor = AppTheme.TextOnAccent,
            Font = AppTheme.GetFont(8F),
            Height = 28,
            Width = 78,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 0),
            UseVisualStyleBackColor = false
        };
        _logoButton.FlatAppearance.BorderColor = AppTheme.AccentLighter;
        _logoButton.FlatAppearance.MouseOverBackColor = AppTheme.AccentLight;
        _logoButton.ForeColor = AppTheme.AccentDarker;
        _logoButton.Click += (_, _) => SetStageMode(StageMode.Logo);

        _hideTextButton = new Button
        {
            Text = "\u25a6  Hide text",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.AccentLighter,
            ForeColor = AppTheme.AccentDarker,
            Font = AppTheme.GetFont(8F),
            Height = 28,
            Width = 96,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 0),
            UseVisualStyleBackColor = false
        };
        _hideTextButton.FlatAppearance.BorderColor = AppTheme.AccentLighter;
        _hideTextButton.FlatAppearance.MouseOverBackColor = AppTheme.AccentLight;
        _hideTextButton.Click += (_, _) => SetStageMode(StageMode.Background);

        _blackButton = new Button
        {
            Text = "\u25fc  Black",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.AccentLighter,
            ForeColor = AppTheme.AccentDarker,
            Font = AppTheme.GetFont(8F),
            Height = 28,
            Width = 78,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 0),
            UseVisualStyleBackColor = false
        };
        _blackButton.FlatAppearance.BorderColor = AppTheme.AccentLighter;
        _blackButton.FlatAppearance.MouseOverBackColor = AppTheme.AccentLight;
        _blackButton.Click += (_, _) => SetStageMode(StageMode.Black);

        actionsPanel.Controls.Add(_blackButton);
        actionsPanel.Controls.Add(_hideTextButton);
        actionsPanel.Controls.Add(_logoButton);
        actionsPanel.Controls.Add(_projectorButton);
        header.Controls.Add(actionsPanel);

        return header;
    }

    // ═══════════════════════════════════════════════════════════════
    //  TEXT TAB RIBBON
    // ═══════════════════════════════════════════════════════════════

    private void BuildTextRibbon(Control page)
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false, BackColor = AppTheme.RibbonBackground, Padding = new Padding(0) };

        // ── Clipboard ───────────────────────────────────────────
        var clipboard = new RibbonGroupPanel("Clipboard");
        _pasteButton = new RibbonSmallButton("Paste", FluentIcons.Paste, width: 56);
        _pasteButton.Click += (_, _) => { if (Clipboard.ContainsText()) _lyricsBox.SelectedText = Clipboard.GetText(); };
        _copyButton = new RibbonSmallButton("Copy", FluentIcons.Copy, width: 52);
        _copyButton.Click += (_, _) => { if (_lyricsBox.SelectedText.Length > 0) Clipboard.SetText(_lyricsBox.SelectedText); };
        _cutButton = new RibbonSmallButton("Cut", FluentIcons.Cut, width: 48);
        _cutButton.Click += (_, _) => { if (_lyricsBox.SelectedText.Length > 0) { Clipboard.SetText(_lyricsBox.SelectedText); _lyricsBox.SelectedText = ""; } };
        var clipTT = new ToolTip();
        clipTT.SetToolTip(_pasteButton, "Paste from clipboard");
        clipTT.SetToolTip(_copyButton, "Copy selected text");
        clipTT.SetToolTip(_cutButton, "Cut selected text");
        clipboard.AddRange(_pasteButton, _cutButton, _copyButton);

        // ── Font ────────────────────────────────────────────────
        var font = new RibbonGroupPanel("Font", 470);
        _fontFamily = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Font = AppTheme.GetFont(8.5F), ForeColor = AppTheme.TextPrimary, BackColor = AppTheme.Surface, FlatStyle = FlatStyle.Flat };
        var fonts = new List<string> { "Segoe UI", "Arial", "Calibri", "Cambria", "Georgia", "Verdana", "Trebuchet MS", "Times New Roman", "Tahoma", "Century Gothic" };
        var telugu = new[] { "Nirmala UI", "Noto Sans Telugu", "Gautami", "Vani", "Lohit Telugu", "Telugu Sangam MN", "Raghu Telugu", "Kalinga", "Shruti", "Tunga", "Malgun Gothic", "Microsoft Himalaya" };
        var installed = new HashSet<string>(FontFamily.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        fonts.AddRange(telugu.Where(installed.Contains));
        _fontFamily.Items.AddRange([.. fonts.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f)]);
        _fontFamily.SelectedItem = _theme.FontFamily;
        _fontFamily.SelectedIndexChanged += (_, _) => { _theme.FontFamily = _fontFamily.Text; RefreshSlides(); };

        _fontSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 48, Font = AppTheme.GetFont(8.5F), ForeColor = AppTheme.TextPrimary, BackColor = AppTheme.Surface, FlatStyle = FlatStyle.Flat };
        foreach (var size in new[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 28, 32, 36, 40, 48, 56, 64, 72, 80, 88, 96 }) _fontSize.Items.Add(size);
        _fontSize.SelectedItem = (int)_theme.FontSize;
        _fontSize.SelectedIndexChanged += (_, _) => { if (_fontSize.SelectedItem is int size) { _theme.FontSize = size; RefreshSlides(); } };

        _boldButton = new RibbonToggleButton("B", AppTheme.GetFont(10F, FontStyle.Bold), "Bold");
        _boldButton.Click += (_, _) => { _theme.Bold = !_boldButton.Toggled; UpdateBoldButton(); RefreshSlides(); };
        _italicButton = new RibbonToggleButton("I", new Font("Georgia", 10F, FontStyle.Italic), "Italic");
        _italicButton.Click += (_, _) => { _theme.Italic = !_italicButton.Toggled; UpdateItalicButton(); RefreshSlides(); };
        _underlineButton = new RibbonToggleButton("U", AppTheme.GetFont(9F, FontStyle.Underline), "Underline");
        _underlineButton.Click += (_, _) => { _theme.Underline = !_underlineButton.Toggled; UpdateUnderlineButton(); RefreshSlides(); };
        _strikethroughButton = new RibbonToggleButton("abc", AppTheme.GetFont(8F, FontStyle.Strikeout), "Strikethrough");
        _strikethroughButton.Click += (_, _) => { _theme.Strikethrough = !_strikethroughButton.Toggled; UpdateStrikethroughButton(); RefreshSlides(); };
        _subscriptButton = new RibbonToggleButton("X\u2082", tooltip: "Subscript");
        _subscriptButton.Click += (_, _) => { _theme.Subscript = !_subscriptButton.Toggled; _theme.Superscript = false; UpdateSubSuperButtons(); RefreshSlides(); };
        _superscriptButton = new RibbonToggleButton("X\u00B2", tooltip: "Superscript");
        _superscriptButton.Click += (_, _) => { _theme.Superscript = !_superscriptButton.Toggled; _theme.Subscript = false; UpdateSubSuperButtons(); RefreshSlides(); };

        _clearFormattingButton = new RibbonSmallButton("Clear", FluentIcons.ClearFormatting, width: 52);
        _clearFormattingButton.Click += (_, _) => ClearFormatting();
        var fontTT = new ToolTip();
        fontTT.SetToolTip(_clearFormattingButton, "Clear all formatting");

        font.Add(new RibbonSeparator(38));
        font.Add(new RibbonComboBoxWrapper(_fontFamily, 130));
        font.Add(new RibbonComboBoxWrapper(_fontSize, 48));
        font.Add(new RibbonSeparator(38));
        font.Add(_boldButton);
        font.Add(_italicButton);
        font.Add(_underlineButton);
        font.Add(_strikethroughButton);
        font.Add(_subscriptButton);
        font.Add(_superscriptButton);
        font.Add(new RibbonSeparator(38));
        font.Add(_clearFormattingButton);

        // ── Paragraph ───────────────────────────────────────────
        var paragraph = new RibbonGroupPanel("Paragraph", 310);
        _alignLeftButton = new RibbonToggleButton("\u2261", tooltip: "Align left");
        _alignLeftButton.Click += (_, _) => SetAlignment("Left");
        _alignCenterButton = new RibbonToggleButton("\u2261", AppTheme.GetFont(10F), "Align centre");
        _alignCenterButton.Click += (_, _) => SetAlignment("Centre");
        _alignRightButton = new RibbonToggleButton("\u2261", tooltip: "Align right");
        _alignRightButton.Click += (_, _) => SetAlignment("Right");
        _alignJustifyButton = new RibbonToggleButton("\u2261", tooltip: "Justify");
        _alignJustifyButton.Click += (_, _) => SetAlignment("Justify");

        // Set initial alignment states
        _alignLeftButton.Toggled = _theme.Alignment == "Left";
        _alignRightButton.Toggled = _theme.Alignment == "Right";
        _alignJustifyButton.Toggled = _theme.Alignment == "Justify";
        _alignCenterButton.Toggled = _theme.Alignment != "Left" && _theme.Alignment != "Right" && _theme.Alignment != "Justify";

        _lineSpacing = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 60, Font = AppTheme.GetFont(8.5F), ForeColor = AppTheme.TextPrimary, BackColor = AppTheme.Surface, FlatStyle = FlatStyle.Flat };
        foreach (var value in new[] { "1.0", "1.15", "1.3", "1.5", "2.0", "3.0" }) _lineSpacing.Items.Add(value);
        var currentSpacing = _lineSpacing.Items.Cast<string>().FirstOrDefault(value => Math.Abs(float.Parse(value, System.Globalization.CultureInfo.InvariantCulture) - _theme.LineSpacing) < 0.001f);
        _lineSpacing.SelectedItem = currentSpacing;
        _lineSpacing.SelectedIndexChanged += (_, _) =>
        {
            if (_lineSpacing.SelectedItem is string value && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var spacing))
            {
                _theme.LineSpacing = spacing;
                RefreshSlides();
            }
        };
        _maxLines = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 4, Width = 42, Font = AppTheme.GetFont(8.5F) };
        _maxLines.ValueChanged += (_, _) => RebuildSlides();

        paragraph.AddRange(_alignLeftButton, _alignCenterButton, _alignRightButton, _alignJustifyButton);
        paragraph.Add(new RibbonSeparator(38));
        paragraph.Add(new RibbonField("Spacing", _lineSpacing, 68));

        // ── Slide layout ────────────────────────────────────────
        var slideLayout = new RibbonGroupPanel("Slide layout", 210);
        _autoFit = new RibbonTinyButton("↔", size: 40, tooltip: "Auto fit lyrics to the slide");
        _autoFit.Active = _theme.AutoFit;
        _autoFit.Click += (_, _) => { _theme.AutoFit = !_autoFit.Active; _autoFit.Active = _theme.AutoFit; SaveBackgroundPreferences(); RefreshSlides(); };
        slideLayout.Add(BuildSlideLayoutFields());

        // ── Colors ──────────────────────────────────────────────
        var colors = new RibbonGroupPanel("Colors", 310);
        _fontColorButton = new RibbonColorButton("A", _theme.TextColor, AppTheme.GetFont(12F, FontStyle.Bold));
        _fontColorButton.Click += (_, _) => ChooseTextColor();
        _textColorPresets.Clear();
        foreach (var color in new[] { Color.White, Color.FromArgb(255, 222, 0), Color.Black, Color.FromArgb(134, 169, 214), Color.FromArgb(119, 163, 66) })
        {
            var preset = new RibbonPresetButton(RibbonPresetKind.TextColor, color, tooltip: $"Use {color.Name} text");
            _textColorPresets.Add((preset, color));
            preset.Click += (_, _) => SetTextColorPreset(color, _textColorPresets);
            colors.Add(preset);
        }
        colors.Add(_fontColorButton);
        UpdateTextColorPresetState(_textColorPresets);

        // ── Animation ───────────────────────────────────────────
        var animation = new RibbonGroupPanel("Animation", 245);
        _transitionPresets.Clear();
        foreach (var (kind, name, tooltip) in new[]
                 {
                     (RibbonPresetKind.NoAnimation, "None", "No transition"),
                     (RibbonPresetKind.CrossFade, "Cross fade", "Cross-fade transition"),
                     (RibbonPresetKind.ZoomIn, "Zoom in", "Zoom-in transition"),
                     (RibbonPresetKind.ZoomOut, "Zoom out", "Zoom-out transition")
                 })
        {
            var preset = new RibbonPresetButton(kind, AppTheme.Accent, tooltip);
            _transitionPresets.Add((preset, name));
            preset.Click += (_, _) => SetSlideTransition(name, _transitionPresets);
            animation.Add(preset);
        }
        UpdateTransitionState(_transitionPresets);

        // ── Styles ──────────────────────────────────────────────
        // ── Cloud sync ──────────────────────────────────────────
        var sync = new RibbonGroupPanel("Cloud sync", 140);
        var syncBtn = new RibbonSmallButton("Sync now", FluentIcons.CloudSync, accent: true, accentBg: AppTheme.Accent, accentFg: AppTheme.TextOnAccent, width: 90);
        syncBtn.Click += async (_, _) =>
        {
            syncBtn.Enabled = false;
            try
            {
                await SyncFromApiAsync();
                _slideStatus.Text = _lastSyncFailed ? $"Sync failed: {_syncError ?? "check connection"}" : "Synced with cloud";
            }
            finally { syncBtn.Enabled = true; }
        };
        sync.Add(syncBtn);

        ribbon.Controls.AddRange([clipboard, font, slideLayout, colors, animation, paragraph, sync]);
        page.Controls.Add(ribbon);
    }

    private Control BuildSlideLayoutFields()
    {
        var fields = new Panel { Width = 190, Height = 58, Margin = new Padding(0, 0, 0, 0), BackColor = AppTheme.RibbonBackground };
        var linesLabel = new Label { Text = "Max. lines", AutoSize = false, Width = 108, Height = 22, Location = new Point(0, 3), Font = AppTheme.GetFont(8.5F), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.MiddleLeft };
        var autoFitLabel = new Label { Text = "Fit lyrics to screen", AutoSize = false, Width = 108, Height = 22, Location = new Point(0, 29), Font = AppTheme.GetFont(8.5F), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.MiddleLeft };
        _maxLines.Location = new Point(125, 2);
        _autoFit.Location = new Point(113, 26);
        fields.Controls.AddRange([linesLabel, autoFitLabel, _maxLines, _autoFit]);
        return fields;
    }

    private void SetTextColorPreset(Color color, IEnumerable<(RibbonPresetButton Button, Color Color)> presets)
    {
        _theme.TextColor = color;
        _fontColorButton.ColorSwatch = color;
        UpdateTextColorPresetState(presets);
        RefreshSlides();
    }

    private void UpdateTextColorPresetState(IEnumerable<(RibbonPresetButton Button, Color Color)> presets)
    {
        foreach (var (button, color) in presets)
            button.Active = color.ToArgb() == _theme.TextColor.ToArgb();
    }

    private void SetSlideTransition(string transition, IEnumerable<(RibbonPresetButton Button, string Name)> presets)
    {
        _theme.SlideTransition = transition;
        UpdateTransitionState(presets);
        SaveBackgroundPreferences();
    }

    private void UpdateTransitionState(IEnumerable<(RibbonPresetButton Button, string Name)> presets)
    {
        foreach (var (button, name) in presets)
            button.Active = string.Equals(name, _theme.SlideTransition, StringComparison.OrdinalIgnoreCase);
    }

    private void SetAlignment(string alignment)
    {
        _theme.Alignment = alignment;
        _alignLeftButton.Toggled = alignment == "Left";
        _alignRightButton.Toggled = alignment == "Right";
        _alignJustifyButton.Toggled = alignment == "Justify";
        _alignCenterButton.Toggled = alignment != "Left" && alignment != "Right" && alignment != "Justify";
        RefreshSlides();
    }

    // ═══════════════════════════════════════════════════════════════
    //  BACKGROUND TAB RIBBON
    // ═══════════════════════════════════════════════════════════════

    private void BuildBackgroundRibbon(Control page)
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false, BackColor = AppTheme.RibbonBackground };

        // ── Colour presets ──────────────────────────────────────
        var colour = new RibbonGroupPanel("Colour", 150);
        foreach (var color in new[]
                 {
                     Color.FromArgb(22, 34, 52), Color.FromArgb(12, 79, 105), Color.FromArgb(74, 33, 95),
                     Color.FromArgb(104, 55, 36), Color.FromArgb(49, 80, 58), Color.FromArgb(238, 240, 243)
                 })
        {
            var swatch = new Panel { Width = 32, Height = 32, BackColor = color, Cursor = Cursors.Hand, Margin = new Padding(1) };
            swatch.Click += (_, _) => SetSolidBackground(color);
            colour.Add(swatch);
        }
        var moreBtn = new Button
        {
            Text = "More\u2026",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.AccentLighter,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.GetFont(8F),
            Height = 32,
            Width = 52,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        moreBtn.FlatAppearance.BorderColor = AppTheme.Border;
        moreBtn.Click += (_, _) => ChooseBackgroundColor();
        colour.Add(moreBtn);

        // ── Image / Video galleries ─────────────────────────────
        _imageGallery = new BackgroundGalleryPanel("Image") { Margin = new Padding(3, 14, 9, 3) };
        _imageGallery.AssetSelected += asset => { if (_updating) return; if (asset is null) SetSolidBackground(Color.Black); else ApplyBackgroundAsset(asset); };
        _imageGallery.ExpandRequested += () => ShowGalleryDropdown("Image", _imageGallery);
        _videoGallery = new BackgroundGalleryPanel("Video") { Margin = new Padding(3, 14, 9, 3) };
        _videoGallery.AssetSelected += asset => { if (_updating) return; if (asset is null) SetSolidBackground(Color.Black); else ApplyBackgroundAsset(asset); };
        _videoGallery.ExpandRequested += () => ShowGalleryDropdown("Video", _videoGallery);

        // ── Brightness ──────────────────────────────────────────
        var brightness = new RibbonGroupPanel("Brightness", 300);
        _brightness = new TrackBar { Minimum = -75, Maximum = 75, Value = _theme.Brightness, TickFrequency = 25, Width = 140, Height = 30, AutoSize = false };
        _brightness.ValueChanged += (_, _) => { _theme.Brightness = _brightness.Value; SaveBackgroundPreferences(); RefreshSlides(); };
        _videoLoop = new CheckBox { Text = "Loop video", Checked = _theme.VideoLoop, AutoSize = true, Font = AppTheme.GetFont(8F), ForeColor = AppTheme.TextPrimary };
        _videoLoop.CheckedChanged += (_, _) => { _theme.VideoLoop = _videoLoop.Checked; SaveBackgroundPreferences(); RefreshSlides(); };
        brightness.AddRange(new RibbonSlider("Brightness", _brightness, 160), _videoLoop);
        RefreshBackgroundGalleries();

        // ── Aspect Ratio (segmented) ────────────────────────────
        var ratio = new RibbonGroupPanel("Aspect ratio", 420);
        var ratioOptions = new[] { "Current", "4:3.2", "4:3", "16:10", "16:9", "9:16", "16:8" };
        var ratioControl = new RibbonSegmentedControl(ratioOptions);
        ratioControl.SelectedIndex = Array.FindIndex(ratioOptions,
            v => string.Equals(v, _theme.AspectRatio, StringComparison.OrdinalIgnoreCase));
        if (ratioControl.SelectedIndex < 0) ratioControl.SelectedIndex = 0;
        ratioControl.SelectionChanged += (_, idx) =>
        {
            if (idx >= 0 && idx < ratioOptions.Length) { _theme.AspectRatio = ratioOptions[idx]; SaveBackgroundPreferences(); RefreshSlides(); }
        };
        ratio.Add(ratioControl);

        ribbon.Controls.AddRange([colour, _imageGallery, _videoGallery, brightness, ratio]);
        page.Controls.Add(ribbon);
    }

    // ═══════════════════════════════════════════════════════════════
    //  BIBLE TAB RIBBON
    // ═══════════════════════════════════════════════════════════════

    private void BuildBibleRibbon(Control page)
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false, BackColor = AppTheme.RibbonBackground };

        // ── Library ─────────────────────────────────────────────
        var library = new RibbonGroupPanel("Library", 220);
        var newBibleBtn = new RibbonSmallButton("New Bible", FluentIcons.NewDocument, width: 70);
        newBibleBtn.Click += (_, _) => NewBible();
        var importBtn = new RibbonSmallButton("Import", FluentIcons.Import, width: 60);
        importBtn.Click += (_, _) => ImportBible();
        var deleteBtn = new RibbonSmallButton("Delete", FluentIcons.Delete, width: 56);
        deleteBtn.Click += (_, _) => DeleteCurrentBible();
        var libTip = new ToolTip();
        libTip.SetToolTip(newBibleBtn, "Start a new Bible translation");
        libTip.SetToolTip(importBtn, "Import a Bible from JSON or SQLite");
        libTip.SetToolTip(deleteBtn, "Delete the current Bible");
        library.AddRange(newBibleBtn, importBtn, deleteBtn);

        // ── Agenda ──────────────────────────────────────────────
        var agenda = new RibbonGroupPanel("Agenda", 130);
        var addVerse = new RibbonSmallButton("Add verse", FluentIcons.AddAgenda, width: 76);
        addVerse.Click += (_, _) => AddSelectedBibleVerseToAgenda();
        agenda.Add(addVerse);

        // ── Present ─────────────────────────────────────────────
        var present = new RibbonGroupPanel("Present", 170);
        var showBtn = new RibbonSmallButton("Show verse", FluentIcons.Screen, accent: true, accentBg: AppTheme.Success, accentFg: Color.White, width: 100);
        showBtn.Click += (_, _) => ShowSelectedBibleVerse();
        var projBtn = new RibbonSmallButton("Open projector", FluentIcons.Projector, accent: true, accentBg: AppTheme.Accent, accentFg: Color.White, width: 110);
        projBtn.Click += (_, _) => ToggleProjector();
        present.AddRange(showBtn, projBtn);

        ribbon.Controls.AddRange([library, agenda, present]);
        page.Controls.Add(ribbon);
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELP TAB RIBBON
    // ═══════════════════════════════════════════════════════════════

    private void BuildHelpRibbon(Control page)
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false, BackColor = AppTheme.RibbonBackground };

        var start = new RibbonGroupPanel("Getting started", 360);
        start.Add(new Label { Text = "1. Open or create a song.  2. Add it to the service agenda.  3. Choose a slide.  4. Press F5 to project.", AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = AppTheme.GetFont(8F), MaximumSize = new Size(340, 0), Margin = new Padding(4) });

        var keyboard = new RibbonGroupPanel("Keyboard shortcuts", 340);
        keyboard.Add(new Label { Text = "Arrows/Page Up/Down: slides.  1-9: verse jump.  F5: projector.  Esc: close.", AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = AppTheme.GetFont(8F), MaximumSize = new Size(320, 0), Margin = new Padding(4) });

        var storage = new RibbonGroupPanel("Library", 260);
        storage.Add(new Label { Text = "Songs, Bibles, and backgrounds are saved automatically.", AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = AppTheme.GetFont(8F), MaximumSize = new Size(240, 0), Margin = new Padding(4) });

        var backup = new RibbonGroupPanel("Backup", 240);
        var exportBtn = new RibbonSmallButton("Export", FluentIcons.Import, accent: true, accentBg: AppTheme.Accent, accentFg: Color.White, width: 80);
        exportBtn.Click += (_, _) => ExportLibrary();
        var importBtn = new RibbonSmallButton("Import", FluentIcons.Import, width: 80);
        importBtn.Click += (_, _) => ImportLibrary();
        backup.AddRange(exportBtn, importBtn);

        var sync = new RibbonGroupPanel("Cloud sync", 360);
        var refreshBtn = new RibbonSmallButton("Refresh now", FluentIcons.SyncArrows, width: 90);
        refreshBtn.Click += async (_, _) => await SyncFromApiAsync();
        sync.Add(refreshBtn);

        ribbon.Controls.AddRange([start, keyboard, storage, backup, sync]);
        page.Controls.Add(ribbon);
    }

    // ═══════════════════════════════════════════════════════════════
    //  STYLE PREVIEW (unchanged logic, new visual)
    // ═══════════════════════════════════════════════════════════════

}
