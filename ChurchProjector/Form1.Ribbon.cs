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
        var tabBar = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.White, Padding = new Padding(10, 0, 0, 0) };
        tabBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(214, 221, 229));
            e.Graphics.DrawLine(pen, 0, tabBar.Height - 1, tabBar.Width, tabBar.Height - 1);
        };
        var backgroundTab = TabButton("Background", false);
        backgroundTab.Dock = DockStyle.Left;
        var bibleTab = TabButton("Bible", false);
        bibleTab.Dock = DockStyle.Left;
        var helpTab = TabButton("Help", false);
        helpTab.Dock = DockStyle.Left;
        var textTab = TabButton("Text", true);
        textTab.Dock = DockStyle.Left;
        tabBar.Controls.Add(helpTab);
        tabBar.Controls.Add(bibleTab);
        tabBar.Controls.Add(backgroundTab);
        tabBar.Controls.Add(textTab);

        _ribbonHost = new Panel { Dock = DockStyle.Top, Height = 148, BackColor = Color.White };
        _ribbonHost.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(214, 221, 229));
            e.Graphics.DrawLine(pen, 0, _ribbonHost.Height - 1, _ribbonHost.Width, _ribbonHost.Height - 1);
        };
        _ribbonHost.Resize += (_, _) => RelayoutRibbon();
        var ribbonHost = _ribbonHost;
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
        RelayoutRibbon();
    }

    private Control BuildStatusBar()
    {
        _statusBar = new StatusStrip
        {
            Dock = DockStyle.Bottom,
            BackColor = _darkBrand,
            ForeColor = Color.White,
            Padding = new Padding(2, 1, 2, 1),
            ShowItemToolTips = false
        };
        var border = ToolStripStatusLabelBorderSides.Right;
        _statusTab = new ToolStripStatusLabel("Text") { ForeColor = Color.White, BorderSides = border };
        _statusSlide = new ToolStripStatusLabel("Slide 1 of 1") { ForeColor = Color.White, BorderSides = border };
        _statusProjector = new ToolStripStatusLabel("Projector: off") { ForeColor = Color.White, BorderSides = border };
        _statusClock = new ToolStripStatusLabel(DateTime.Now.ToShortTimeString()) { ForeColor = Color.White, Spring = true, TextAlign = ContentAlignment.MiddleRight };
        _statusSync = new ToolStripStatusLabel("\u25cb Connecting...") { ForeColor = Color.FromArgb(255, 220, 120), BorderSides = border };
        _statusBar.Items.AddRange([_statusTab, _statusSlide, _statusProjector, _statusSync, _statusClock]);
        return _statusBar;
    }

    private void SwitchMode(Control ribbon, Control workspace, Button active, params Button[] inactive)
    {
        foreach (Control control in ribbon.Parent!.Controls) control.Visible = control == ribbon;
        foreach (Control control in workspace.Parent!.Controls) control.Visible = control == workspace;
        SetTabState(active, true);
        foreach (var button in inactive) SetTabState(button, false);
        if (_statusTab is not null)
            _statusTab.Text = "Tab: " + active.Text;
        _currentTab = active.Text.Contains("Bible") ? "bible" : active.Text.Contains("Background") ? "background" : active.Text.Contains("Help") ? "help" : "text";
        _activeAgendaList = _currentTab == "bible" ? _bibleAgendaList : null;
        RelayoutRibbon();
    }

    /// <summary>
    /// Sizes the ribbon host to fit the visible ribbon, wrapping to two rows when the
    /// window is narrow — the same behaviour as the Word ribbon.
    /// </summary>
    private void RelayoutRibbon()
    {
        if (_ribbonHost is null) return;
        var ribbon = _ribbonHost.Controls.OfType<FlowLayoutPanel>().FirstOrDefault(control => control.Visible);
        if (ribbon is null) return;
        var total = 0;
        var rowHeight = 0;
        foreach (Control child in ribbon.Controls)
        {
            total += child.Width + child.Margin.Horizontal;
            rowHeight = Math.Max(rowHeight, child.Height + child.Margin.Vertical);
        }
        var available = Math.Max(240, _ribbonHost.ClientSize.Width - 24);
        var rows = total <= available ? 1 : 2;
        var target = rows * rowHeight + 14;
        if (Math.Abs(_ribbonHost.Height - target) >= 3) _ribbonHost.Height = target;
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
        var subtitle = new Label { Text = "Sunday service · Ready", ForeColor = Color.FromArgb(213, 235, 251), AutoSize = true, Anchor = AnchorStyles.None, Margin = new Padding(0, 2, 0, 0) };
        brand.Controls.Add(star, 0, 0);
        brand.Controls.Add(title, 1, 0);
        brand.Controls.Add(subtitle, 2, 0);
        header.Controls.Add(brand);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Height = 48, Padding = new Padding(0, 8, 14, 0), WrapContents = false, BackColor = _brand };
        _blackButton = Button("◼  Black", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 84, 31);
        _blackButton.Click += (_, _) => SetStageMode(StageMode.Black);
        _hideTextButton = Button("▦  Hide text", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 104, 31);
        _hideTextButton.Click += (_, _) => SetStageMode(StageMode.Background);
        _logoButton = Button("✦  Logo", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 84, 31);
        _logoButton.Click += (_, _) => SetStageMode(StageMode.Logo);
        _projectorButton = Button("▣  Open projector", Color.FromArgb(11, 77, 132), Color.White, 162, 31);
        _projectorButton.Click += (_, _) => ToggleProjector();
        actions.Controls.AddRange([_blackButton, _hideTextButton, _logoButton, _projectorButton]);
        header.Controls.Add(actions);
        return header;
    }

    private Control BuildTextRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = false, BackColor = Color.White, Padding = new Padding(8, 4, 8, 4) };

        // Clipboard group
        var clipboard = RibbonGroup("Clipboard", 150);
        _pasteButton = RibbonButton(RibbonGlyph.Paste, "Paste", 46, 62);
        _pasteButton.Click += (_, _) => { if (Clipboard.ContainsText()) _lyricsBox.SelectedText = Clipboard.GetText(); };
        _copyButton = RibbonButton(RibbonGlyph.Copy, "Copy", 32, 54);
        _copyButton.Click += (_, _) => { if (_lyricsBox.SelectedText.Length > 0) Clipboard.SetText(_lyricsBox.SelectedText); };
        _cutButton = RibbonButton(RibbonGlyph.Cut, "Cut", 32, 54);
        _cutButton.Click += (_, _) => { if (_lyricsBox.SelectedText.Length > 0) Clipboard.SetText(_lyricsBox.SelectedText); _lyricsBox.SelectedText = ""; };
        var clipTT = new ToolTip();
        clipTT.SetToolTip(_pasteButton, "Paste from clipboard");
        clipTT.SetToolTip(_copyButton, "Copy selected text");
        clipTT.SetToolTip(_cutButton, "Cut selected text");
        Add(clipboard, _pasteButton, _copyButton, _cutButton);

        // Font group
        var font = RibbonGroup("Font", 330);
        _fontFamily = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Margin = new Padding(0, 2, 6, 0) };
        var fonts = new List<string> { "Segoe UI", "Arial", "Calibri", "Cambria", "Georgia", "Verdana", "Trebuchet MS", "Times New Roman", "Tahoma", "Century Gothic" };
        // Telugu fonts that support Telugu script rendering
        var telugu = new[] { "Nirmala UI", "Noto Sans Telugu", "Gautami", "Vani", "Lohit Telugu", "Telugu Sangam MN", "Raghu Telugu", "Kalinga", "Shruti", "Tunga", "Malgun Gothic", "Microsoft Himalaya" };
        var installed = new HashSet<string>(FontFamily.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        fonts.AddRange(telugu.Where(installed.Contains));
        _fontFamily.Items.AddRange([.. fonts.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f)]);
        _fontFamily.SelectedItem = _theme.FontFamily;
        _fontFamily.SelectedIndexChanged += (_, _) => { _theme.FontFamily = _fontFamily.Text; RefreshSlides(); };

        _fontSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 50, Margin = new Padding(0, 2, 6, 0) };
        foreach (var size in new[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 28, 32, 36, 40, 48, 56, 64, 72, 80, 88, 96 }) _fontSize.Items.Add(size);
        _fontSize.SelectedItem = (int)_theme.FontSize;
        _fontSize.SelectedIndexChanged += (_, _) => { if (_fontSize.SelectedItem is int size) { _theme.FontSize = size; RefreshSlides(); } };

        _boldButton = ToggleStyleButton("B", new Font("Segoe UI", 10.5F, FontStyle.Bold), "Bold");
        _boldButton.Click += (_, _) => { _theme.Bold = !_theme.Bold; UpdateBoldButton(); RefreshSlides(); };
        _italicButton = ToggleStyleButton("I", new Font("Georgia", 10.5F, FontStyle.Italic), "Italic");
        _italicButton.Click += (_, _) => { _theme.Italic = !_theme.Italic; UpdateItalicButton(); RefreshSlides(); };
        _underlineButton = ToggleStyleButton("U", new Font("Segoe UI", 9.5F, FontStyle.Underline), "Underline");
        _underlineButton.Click += (_, _) => { _theme.Underline = !_theme.Underline; UpdateUnderlineButton(); RefreshSlides(); };
        _strikethroughButton = ToggleStyleButton("abc", new Font("Segoe UI", 8.5F, FontStyle.Strikeout), "Strikethrough");
        _strikethroughButton.Click += (_, _) => { _theme.Strikethrough = !_theme.Strikethrough; UpdateStrikethroughButton(); RefreshSlides(); };
        _subscriptButton = ToggleStyleButton("X₂", new Font("Segoe UI", 9F), "Subscript");
        _subscriptButton.Click += (_, _) => { _theme.Subscript = !_theme.Subscript; _theme.Superscript = false; UpdateSubSuperButtons(); RefreshSlides(); };
        _superscriptButton = ToggleStyleButton("X²", new Font("Segoe UI", 9F), "Superscript");
        _superscriptButton.Click += (_, _) => { _theme.Superscript = !_theme.Superscript; _theme.Subscript = false; UpdateSubSuperButtons(); RefreshSlides(); };
        _fontColorButton = new ColorButton("A", _theme.TextColor, new Font("Segoe UI", 13F, FontStyle.Bold)) { Width = 32, Height = 42 };
        _fontColorButton.Click += (_, _) => ChooseTextColor();
        _highlightColorButton = new ColorButton("▱", _theme.HighlightColor == Color.Transparent ? Color.FromArgb(255, 255, 153) : _theme.HighlightColor, new Font("Segoe UI", 13F, FontStyle.Bold)) { Width = 32, Height = 42 };
        _highlightColorButton.Click += (_, _) => ChooseHighlightColor();
        _clearFormattingButton = RibbonButton(RibbonGlyph.ClearFormatting, "Clear", 34, 54);
        _clearFormattingButton.Click += (_, _) => ClearFormatting();
        var fontTT = new ToolTip();
        fontTT.SetToolTip(_clearFormattingButton, "Clear bold, italic, underline, strikethrough and colour");
        Add(font, _fontFamily, _fontSize, _boldButton, _italicButton, _underlineButton, _strikethroughButton, _subscriptButton, _superscriptButton, _fontColorButton, _highlightColorButton, _clearFormattingButton);

        // Paragraph group - alignment icons, lists, indent, spacing
        var paragraph = RibbonGroup("Paragraph", 300);
        _alignLeftButton = AlignButton(RibbonGlyph.AlignLeft, "Align left");
        _alignLeftButton.Click += (_, _) => SetAlignment("Left");
        _alignCenterButton = AlignButton(RibbonGlyph.AlignCenter, "Align centre");
        _alignCenterButton.Click += (_, _) => SetAlignment("Centre");
        _alignRightButton = AlignButton(RibbonGlyph.AlignRight, "Align right");
        _alignRightButton.Click += (_, _) => SetAlignment("Right");
        _alignJustifyButton = AlignButton(RibbonGlyph.AlignJustify, "Justify");
        _alignJustifyButton.Click += (_, _) => SetAlignment("Justify");
        _alignLeftButton.Highlighted = _theme.Alignment == "Left";
        _alignRightButton.Highlighted = _theme.Alignment == "Right";
        _alignJustifyButton.Highlighted = _theme.Alignment == "Justify";
        _alignCenterButton.Highlighted = _theme.Alignment != "Left" && _theme.Alignment != "Right" && _theme.Alignment != "Justify";
        _bulletsButton = RibbonButton(RibbonGlyph.Bullets, "Bullets", 32, 54);
        _bulletsButton.Click += (_, _) => ToggleBullets();
        _numberingButton = RibbonButton(RibbonGlyph.Numbered, "Number", 32, 54);
        _numberingButton.Click += (_, _) => ToggleNumbering();
        _decreaseIndentButton = RibbonButton(RibbonGlyph.IndentLeft, "Less", 32, 54);
        _decreaseIndentButton.Click += (_, _) => AdjustIndent(-1);
        _increaseIndentButton = RibbonButton(RibbonGlyph.IndentRight, "More", 32, 54);
        _increaseIndentButton.Click += (_, _) => AdjustIndent(1);
        _lineSpacing = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 74, Margin = new Padding(0, 1, 0, 0) };
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
        _maxLines = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 4, Width = 44, Margin = new Padding(0, 1, 0, 0) };
        _maxLines.ValueChanged += (_, _) => RebuildSlides();
        Add(paragraph, _alignLeftButton, _alignCenterButton, _alignRightButton, _alignJustifyButton, _bulletsButton, _numberingButton, _decreaseIndentButton, _increaseIndentButton, MiniField("Spacing", _lineSpacing), MiniField("Lines", _maxLines));

        // Styles group - quick text styles
        var styles = RibbonGroup("Styles", 274);
        var tt = new ToolTip();
        var lightPreview = PreviewStyle("Light", Color.White, Color.FromArgb(28, 34, 45), () => SetTextStyle(Color.White, true));
        tt.SetToolTip(lightPreview, "Light text — white on dark background");
        var warmPreview = PreviewStyle("Warm", Color.FromArgb(255, 239, 171), Color.FromArgb(71, 48, 40), () => SetTextStyle(Color.FromArgb(255, 239, 171), true));
        tt.SetToolTip(warmPreview, "Warm text — warm cream with dark text");
        var darkPreview = PreviewStyle("Dark", Color.FromArgb(28, 40, 52), Color.FromArgb(232, 240, 245), () => SetTextStyle(Color.FromArgb(28, 40, 52), false));
        tt.SetToolTip(darkPreview, "Dark text — dark background with light text");
        var titlePreview = PreviewStyle("Title", Color.FromArgb(255, 210, 64), Color.FromArgb(22, 34, 52), () => SetTextStyle(Color.FromArgb(255, 210, 64), true));
        tt.SetToolTip(titlePreview, "Title style — golden accent color");
        Add(styles, lightPreview, warmPreview, darkPreview, titlePreview);

        // Cloud sync group
        var sync = RibbonGroup("Cloud sync", 136);
        var syncNow = RibbonActionButton("\u21bb  Sync now", _brand, Color.White, 108, 30);
        syncNow.Click += async (_, _) =>
        {
            syncNow.Enabled = false;
            syncNow.Text = "\u21bb  Syncing...";
            try
            {
                await SyncFromApiAsync();
                _slideStatus.Text = _lastSyncFailed ? $"Sync failed: {_syncError ?? "check connection"}" : "Synced with cloud";
            }
            finally
            {
                syncNow.Text = "\u21bb  Sync now";
                syncNow.Enabled = true;
            }
        };
        Add(sync, syncNow, Hint("Sync songs with the web app at mph-songs.vercel.app"));

        ribbon.Controls.AddRange([clipboard, font, paragraph, styles, sync]);
        return ribbon;
    }

    private void SetAlignment(string alignment)
    {
        _theme.Alignment = alignment;
        _alignLeftButton.Highlighted = alignment == "Left";
        _alignRightButton.Highlighted = alignment == "Right";
        _alignJustifyButton.Highlighted = alignment == "Justify";
        _alignCenterButton.Highlighted = alignment != "Left" && alignment != "Right" && alignment != "Justify";
        RefreshSlides();
    }

    private Control BuildBackgroundRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        var colour = RibbonGroup("Colour", 330);
        foreach (var color in new[]
                 {
                     Color.FromArgb(22, 34, 52), Color.FromArgb(12, 79, 105), Color.FromArgb(74, 33, 95),
                     Color.FromArgb(104, 55, 36), Color.FromArgb(49, 80, 58), Color.FromArgb(238, 240, 243)
                 })
        {
            var swatch = Button("", color, Color.White, 36, 36);
            swatch.Click += (_, _) => SetSolidBackground(color);
            Add(colour, swatch);
        }
        var more = Button("More…", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 56, 36);
        more.Click += (_, _) => ChooseBackgroundColor();
        Add(colour, more);

        _imageGallery = new BackgroundGalleryPanel("Image") { Margin = new Padding(3, 14, 9, 3) };
        _imageGallery.AssetSelected += asset => { if (_updating) return; if (asset is null) SetSolidBackground(Color.Black); else ApplyBackgroundAsset(asset); };
        _imageGallery.ExpandRequested += () => ShowGalleryDropdown("Image", _imageGallery);
        _videoGallery = new BackgroundGalleryPanel("Video") { Margin = new Padding(3, 14, 9, 3) };
        _videoGallery.AssetSelected += asset => { if (_updating) return; if (asset is null) SetSolidBackground(Color.Black); else ApplyBackgroundAsset(asset); };
        _videoGallery.ExpandRequested += () => ShowGalleryDropdown("Video", _videoGallery);

        var brightness = RibbonGroup("Brightness", 320);
        var sun = new Label { Text = "☀", ForeColor = Color.FromArgb(240, 180, 30), Font = new Font("Segoe UI", 14F), AutoSize = true, Margin = new Padding(0, 2, 2, 0) };
        var minus = new Label { Text = "−", ForeColor = Color.FromArgb(110, 120, 132), AutoSize = true, Margin = new Padding(2, 7, 2, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        var plus = new Label { Text = "+", ForeColor = Color.FromArgb(110, 120, 132), AutoSize = true, Margin = new Padding(2, 7, 0, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        _brightness = new TrackBar { Minimum = -75, Maximum = 75, Value = _theme.Brightness, TickFrequency = 25, Width = 120, Height = 30, AutoSize = false };
        _brightness.ValueChanged += (_, _) => { _theme.Brightness = _brightness.Value; SaveBackgroundPreferences(); RefreshSlides(); };
        _videoLoop = new CheckBox { Text = "Loop video", Checked = _theme.VideoLoop, AutoSize = true, Margin = new Padding(8, 7, 0, 0) };
        _videoLoop.CheckedChanged += (_, _) => { _theme.VideoLoop = _videoLoop.Checked; SaveBackgroundPreferences(); RefreshSlides(); };
        Add(brightness, sun, minus, _brightness, plus, _videoLoop);
        RefreshBackgroundGalleries();

        var animation = RibbonGroup("Animation", 300);
        var animNone = Button("▢", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 56, 56);
        animNone.Font = new Font("Segoe UI", 14F);
        animNone.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 227);
        var animOverlap = Button("▣", Color.FromArgb(200, 205, 210), Color.FromArgb(31, 48, 68), 56, 56);
        animOverlap.Font = new Font("Segoe UI", 14F);
        animOverlap.FlatAppearance.BorderColor = Color.FromArgb(160, 170, 180);
        var animOut1 = Button("⤢", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 56, 56);
        animOut1.Font = new Font("Segoe UI", 12F);
        var animOut2 = Button("⤡", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 56, 56);
        animOut2.Font = new Font("Segoe UI", 12F);
        var animTip = new ToolTip();
        animTip.SetToolTip(animNone, "No animation");
        animTip.SetToolTip(animOverlap, "Cross fade (selected)");
        animTip.SetToolTip(animOut1, "Zoom in");
        animTip.SetToolTip(animOut2, "Zoom out");
        Add(animation, animNone, animOverlap, animOut1, animOut2);

        var ratio = RibbonGroup("Aspect ratio", 430);
        foreach (var value in new[] { "Current", "4:3.2", "4:3", "16:10", "16:9", "16:8" })
        {
            var isCurrent = string.Equals(value, _theme.AspectRatio, StringComparison.OrdinalIgnoreCase) || (value == "Current" && string.Equals(_theme.AspectRatio, "Current", StringComparison.OrdinalIgnoreCase));
            var w = value == "Current" ? 66 : 56;
            var choice = Button(value, isCurrent ? _brand : Color.White, isCurrent ? Color.White : Color.FromArgb(31, 48, 68), w, 32);
            choice.Click += (_, _) => SetAspectRatio(value, ratio);
            Add(ratio, choice);
        }
        ribbon.Controls.AddRange([colour, _imageGallery, _videoGallery, brightness, animation, ratio]);
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
