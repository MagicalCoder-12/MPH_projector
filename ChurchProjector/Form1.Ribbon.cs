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

        _ribbonHost = new Panel { Dock = DockStyle.Top, Height = 160, BackColor = Color.White, Padding = new Padding(10, 8, 10, 8) };
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
        _activeAgendaList = _currentTab == "bible" ? _bibleAgendaList : null;
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
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        
        // Clipboard group (Cut, Copy, Paste)
        var clipboard = RibbonGroup("Clipboard", 160);
        _cutButton = Button("✂", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _cutButton.Font = new Font("Segoe UI", 10F);
        _cutButton.Click += (_, _) => { if (_lyricsBox.SelectedText.Length > 0) Clipboard.SetText(_lyricsBox.SelectedText); _lyricsBox.SelectedText = ""; };
        _copyButton = Button("📋", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _copyButton.Font = new Font("Segoe UI", 10F);
        _copyButton.Click += (_, _) => { if (_lyricsBox.SelectedText.Length > 0) Clipboard.SetText(_lyricsBox.SelectedText); };
        _pasteButton = Button("📄", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _pasteButton.Font = new Font("Segoe UI", 10F);
        _pasteButton.Click += (_, _) => { if (Clipboard.ContainsText()) _lyricsBox.SelectedText = Clipboard.GetText(); };
        var clipTT = new ToolTip();
        clipTT.SetToolTip(_cutButton, "Cut selected text");
        clipTT.SetToolTip(_copyButton, "Copy selected text");
        clipTT.SetToolTip(_pasteButton, "Paste from clipboard");
        Add(clipboard, _cutButton, _copyButton, _pasteButton);

        // Font group - comprehensive font controls like Word
        var font = RibbonGroup("Font", 540);
        _fontFamily = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        var fonts = new List<string> { "Segoe UI", "Arial", "Calibri", "Cambria", "Georgia", "Verdana", "Trebuchet MS", "Times New Roman", "Tahoma", "Century Gothic" };
        // Telugu fonts that support Telugu script rendering
        var telugu = new[] { "Nirmala UI", "Noto Sans Telugu", "Gautami", "Vani", "Lohit Telugu", "Telugu Sangam MN", "Raghu Telugu", "Kalinga", "Shruti", "Tunga", "Malgun Gothic", "Microsoft Himalaya" };
        var installed = new HashSet<string>(FontFamily.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        fonts.AddRange(telugu.Where(installed.Contains));
        _fontFamily.Items.AddRange([.. fonts.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f)]);
        _fontFamily.SelectedItem = _theme.FontFamily;
        _fontFamily.SelectedIndexChanged += (_, _) => { _theme.FontFamily = _fontFamily.Text; RefreshSlides(); };
        
        _fontSize = new NumericUpDown { Minimum = 8, Maximum = 200, Value = 56, Width = 53 };
        _fontSize.ValueChanged += (_, _) => { _theme.FontSize = (float)_fontSize.Value; RefreshSlides(); };
        
        _boldButton = Button("B", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _boldButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _boldButton.Click += (_, _) => { _theme.Bold = !_theme.Bold; UpdateBoldButton(); RefreshSlides(); };
        
        _italicButton = Button("I", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _italicButton.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
        _italicButton.Click += (_, _) => { _theme.Italic = !_theme.Italic; UpdateItalicButton(); RefreshSlides(); };
        
        _underlineButton = Button("U", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _underlineButton.Font = new Font("Segoe UI", 10F, FontStyle.Underline);
        _underlineButton.Click += (_, _) => { _theme.Underline = !_theme.Underline; UpdateUnderlineButton(); RefreshSlides(); };
        
        _strikethroughButton = Button("abc", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _strikethroughButton.Font = new Font("Segoe UI", 8F, FontStyle.Strikeout);
        _strikethroughButton.Click += (_, _) => { _theme.Strikethrough = !_theme.Strikethrough; UpdateStrikethroughButton(); RefreshSlides(); };
        
        _subscriptButton = Button("X₂", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _subscriptButton.Font = new Font("Segoe UI", 8F);
        _subscriptButton.Click += (_, _) => { _theme.Subscript = !_theme.Subscript; _theme.Superscript = false; UpdateSubSuperButtons(); RefreshSlides(); };
        
        _superscriptButton = Button("X²", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _superscriptButton.Font = new Font("Segoe UI", 8F);
        _superscriptButton.Click += (_, _) => { _theme.Superscript = !_theme.Superscript; _theme.Subscript = false; UpdateSubSuperButtons(); RefreshSlides(); };
        
        _fontColorButton = Button("A", _theme.TextColor, Color.White, 36, 29);
        _fontColorButton.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
        _fontColorButton.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _fontColorButton.Click += (_, _) => ChooseTextColor();
        
        _highlightColorButton = Button("▱", Color.FromArgb(255, 255, 153), Color.FromArgb(31, 48, 68), 36, 29);
        _highlightColorButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _highlightColorButton.Click += (_, _) => ChooseHighlightColor();
        
        _clearFormattingButton = Button("A⃠", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _clearFormattingButton.Font = new Font("Segoe UI", 9F);
        _clearFormattingButton.Click += (_, _) => ClearFormatting();
        
        Add(font, _fontFamily, _fontSize, _boldButton, _italicButton, _underlineButton, _strikethroughButton, _subscriptButton, _superscriptButton, _fontColorButton, _highlightColorButton, _clearFormattingButton);

        // Paragraph group - alignment and spacing
        var paragraph = RibbonGroup("Paragraph", 380);
        _alignment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 105 };
        _alignment.Items.AddRange(["Left", "Centre", "Right", "Justify"]);
        _alignment.SelectedItem = "Centre";
        _alignment.SelectedIndexChanged += (_, _) => { _theme.Alignment = _alignment.Text; RefreshSlides(); };
        
        _bulletsButton = Button("≡", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _bulletsButton.Font = new Font("Segoe UI", 10F);
        _bulletsButton.Click += (_, _) => ToggleBullets();
        
        _numberingButton = Button("1.", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _numberingButton.Font = new Font("Segoe UI", 9F);
        _numberingButton.Click += (_, _) => ToggleNumbering();
        
        _decreaseIndentButton = Button("➤", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _decreaseIndentButton.Font = new Font("Segoe UI", 8F);
        _decreaseIndentButton.Click += (_, _) => AdjustIndent(-1);
        
        _increaseIndentButton = Button("➤", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _increaseIndentButton.Font = new Font("Segoe UI", 8F);
        _increaseIndentButton.Click += (_, _) => AdjustIndent(1);
        
        _lineSpacing = new NumericUpDown { Minimum = 1, Maximum = 5, Value = 1, Increment = 0.1m, Width = 48 };
        _lineSpacing.ValueChanged += (_, _) => { _theme.LineSpacing = (float)_lineSpacing.Value; RefreshSlides(); };
        
        _maxLines = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 4, Width = 48 };
        _maxLines.ValueChanged += (_, _) => RebuildSlides();
        
        Add(paragraph, Field("Align", _alignment), _bulletsButton, _numberingButton, _decreaseIndentButton, _increaseIndentButton, Field("Spacing", _lineSpacing), Field("Max lines", _maxLines));

        // Styles group - quick text styles
        var styles = RibbonGroup("Styles", 380);
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

        // Cloud Sync group
        var sync = RibbonGroup("Cloud sync", 300);
        var syncNow = Button("\u21bb  Sync now", _brand, Color.White, 100, 34);
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
