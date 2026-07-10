using System.Drawing.Drawing2D;

namespace ChurchProjector;

public partial class Form1 : Form
{
    private readonly PresentationTheme _theme = new();
    private readonly List<string> _slides = [];
    private readonly List<Song> _library =
    [
        new("Great Is Your Faithfulness", "Great is Your faithfulness, O God my Father\nThere is no shadow of turning with You\n\nYou never change, You are compassionate\nAll that You are is forever true\n\nGreat is Your faithfulness\nGreat is Your faithfulness\nMorning by morning new mercies I see\n\nAll I have needed Your hand has provided\nGreat is Your faithfulness, Lord, unto me"),
        new("Way Maker", "You are here, moving in our midst\nI worship You, I worship You\n\nYou are here, working in this place\nI worship You, I worship You\n\nWay Maker, miracle worker\nPromise keeper, light in the darkness\nMy God, that is who You are"),
        new("Amazing Grace", "Amazing grace, how sweet the sound\nThat saved a wretch like me\n\nI once was lost, but now am found\nWas blind, but now I see")
    ];

    private readonly List<string> _agenda = [];
    private RichTextBox _lyricsBox = null!;
    private TextBox _titleBox = null!;
    private ListBox _slideList = null!;
    private ListBox _agendaList = null!;
    private ListBox _libraryList = null!;
    private Label _slideStatus = null!;
    private SlideCanvas _audiencePreview = null!;
    private ComboBox _fontFamily = null!;
    private NumericUpDown _fontSize = null!;
    private Button _boldButton = null!;
    private Button _fontColorButton = null!;
    private ComboBox _alignment = null!;
    private NumericUpDown _maxLines = null!;
    private TrackBar _brightness = null!;
    private ProjectorForm? _projector;
    private int _currentSlide;
    private bool _updating;

    private readonly Color _brand = Color.FromArgb(22, 113, 180);
    private readonly Color _darkBrand = Color.FromArgb(14, 83, 143);
    private readonly Color _panelBorder = Color.FromArgb(210, 218, 227);

    public Form1()
    {
        InitializeComponent();
        BuildInterface();
        LoadSong(_library[0]);
    }

    private void BuildInterface()
    {
        SuspendLayout();
        Text = "Church Projector";
        MinimumSize = new Size(1120, 720);
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(241, 244, 247);
        Font = new Font("Segoe UI", 9F);

        var header = BuildHeader();
        var tabBar = BuildTabBar();
        var ribbonHost = new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Color.White, Padding = new Padding(10, 8, 10, 8) };
        var textRibbon = BuildTextRibbon();
        var backgroundRibbon = BuildBackgroundRibbon();
        ribbonHost.Controls.Add(textRibbon);
        ribbonHost.Controls.Add(backgroundRibbon);
        backgroundRibbon.Visible = false;

        Controls.Add(BuildWorkspace());
        Controls.Add(ribbonHost);
        Controls.Add(tabBar);
        Controls.Add(header);

        void ShowRibbon(Control visible, Button active, Button inactive)
        {
            textRibbon.Visible = visible == textRibbon;
            backgroundRibbon.Visible = visible == backgroundRibbon;
            active.BackColor = Color.White;
            active.ForeColor = _darkBrand;
            inactive.BackColor = _darkBrand;
            inactive.ForeColor = Color.White;
        }

        var textTab = (Button)tabBar.Tag!;
        var backgroundTab = (Button)tabBar.Controls[0];
        textTab.Click += (_, _) => ShowRibbon(textRibbon, textTab, backgroundTab);
        backgroundTab.Click += (_, _) => ShowRibbon(backgroundRibbon, backgroundTab, textTab);
        ResumeLayout();
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = _brand };
        var mark = new Label
        {
            Text = "✦",
            Font = new Font("Segoe UI Symbol", 22F, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 210, 64),
            Location = new Point(14, 7),
            AutoSize = true
        };
        var title = new Label
        {
            Text = "CHURCH PROJECTOR",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Location = new Point(50, 12),
            AutoSize = true
        };
        var subtitle = new Label
        {
            Text = "Sunday service · Ready",
            ForeColor = Color.FromArgb(213, 235, 251),
            Location = new Point(232, 15),
            AutoSize = true
        };
        var projector = CreateButton("▣  Open projector", Color.FromArgb(11, 77, 132), Color.White, 154, 31);
        projector.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        projector.Location = new Point(Width - 184, 8);
        projector.Click += (_, _) => ToggleProjector();
        header.Resize += (_, _) => projector.Left = header.ClientSize.Width - projector.Width - 16;
        header.Controls.AddRange([mark, title, subtitle, projector]);
        return header;
    }

    private Control BuildTabBar()
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = _darkBrand, Padding = new Padding(10, 5, 0, 5) };
        var background = CreateTabButton("▧  Background", false);
        background.Dock = DockStyle.Left;
        var text = CreateTabButton("A  Text", true);
        text.Dock = DockStyle.Left;
        bar.Controls.Add(background);
        bar.Controls.Add(text);
        bar.Tag = text;
        return bar;
    }

    private Control BuildTextRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        var fontGroup = CreateRibbonGroup("Font");
        _fontFamily = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145, Margin = new Padding(0, 0, 6, 0) };
        _fontFamily.Items.AddRange(["Segoe UI", "Arial", "Calibri", "Georgia", "Verdana", "Trebuchet MS"]);
        _fontFamily.SelectedItem = _theme.FontFamily;
        _fontFamily.SelectedIndexChanged += (_, _) => { _theme.FontFamily = _fontFamily.Text; RefreshSlides(); };
        _fontSize = new NumericUpDown { Minimum = 18, Maximum = 130, Value = 56, Width = 58 };
        _fontSize.ValueChanged += (_, _) => { _theme.FontSize = (float)_fontSize.Value; RefreshSlides(); };
        _boldButton = CreateButton("B", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 36, 29);
        _boldButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _boldButton.Click += (_, _) => { _theme.Bold = !_theme.Bold; UpdateBoldButton(); RefreshSlides(); };
        _fontColorButton = CreateButton("A", _theme.TextColor, Color.White, 36, 29);
        _fontColorButton.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _fontColorButton.Click += (_, _) => ChooseTextColor();
        fontGroup.Controls.AddRange([_fontFamily, _fontSize, _boldButton, _fontColorButton]);

        var layoutGroup = CreateRibbonGroup("Slide layout");
        _alignment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        _alignment.Items.AddRange(["Left", "Centre", "Right"]);
        _alignment.SelectedItem = "Centre";
        _alignment.SelectedIndexChanged += (_, _) => { _theme.Alignment = _alignment.Text; RefreshSlides(); };
        _maxLines = new NumericUpDown { Minimum = 1, Maximum = 12, Value = 4, Width = 50 };
        _maxLines.ValueChanged += (_, _) => RebuildSlides();
        layoutGroup.Controls.AddRange([
            MakeField("Align", _alignment),
            MakeField("Max. lines", _maxLines),
            HintLabel("Separate verses with a blank line")
        ]);

        var quickGroup = CreateRibbonGroup("Quick style");
        quickGroup.Controls.Add(CreatePresetButton("Light text", Color.White, Color.FromArgb(28, 34, 45), () => SetTextStyle(Color.White, true)));
        quickGroup.Controls.Add(CreatePresetButton("Warm text", Color.FromArgb(255, 239, 171), Color.FromArgb(71, 48, 40), () => SetTextStyle(Color.FromArgb(255, 239, 171), true)));
        quickGroup.Controls.Add(CreatePresetButton("Dark text", Color.FromArgb(28, 40, 52), Color.FromArgb(232, 240, 245), () => SetTextStyle(Color.FromArgb(28, 40, 52), false)));

        ribbon.Controls.AddRange([fontGroup, layoutGroup, quickGroup]);
        return ribbon;
    }

    private Control BuildBackgroundRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        var colorGroup = CreateRibbonGroup("Colour");
        foreach (var color in new[]
                 {
                     Color.FromArgb(22, 34, 52), Color.FromArgb(12, 79, 105), Color.FromArgb(74, 33, 95),
                     Color.FromArgb(104, 55, 36), Color.FromArgb(49, 80, 58), Color.FromArgb(238, 240, 243)
                 })
        {
            var swatch = CreateButton("", color, Color.White, 36, 36);
            swatch.FlatAppearance.BorderColor = Color.FromArgb(184, 194, 204);
            swatch.Click += (_, _) => { _theme.BackgroundColor = color; _theme.BackgroundImage = null; RefreshSlides(); };
            colorGroup.Controls.Add(swatch);
        }
        var moreColor = CreateButton("More…", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 58, 36);
        moreColor.Click += (_, _) => ChooseBackgroundColor();
        colorGroup.Controls.Add(moreColor);

        var imageGroup = CreateRibbonGroup("Image");
        var customImage = CreateButton("Choose image…", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 104, 34);
        customImage.Click += (_, _) => ChooseBackgroundImage();
        var removeImage = CreateButton("Remove image", Color.White, Color.FromArgb(31, 48, 68), 95, 34);
        removeImage.Click += (_, _) => { _theme.BackgroundImage = null; RefreshSlides(); };
        imageGroup.Controls.AddRange([customImage, removeImage, HintLabel("Images fill the slide")]);

        var brightnessGroup = CreateRibbonGroup("Brightness");
        _brightness = new TrackBar { Minimum = -75, Maximum = 75, Value = 0, TickFrequency = 25, Width = 145, Height = 38 };
        _brightness.ValueChanged += (_, _) => { _theme.Brightness = _brightness.Value; RefreshSlides(); };
        brightnessGroup.Controls.Add(_brightness);

        var ratioGroup = CreateRibbonGroup("Aspect ratio");
        foreach (var ratio in new[] { "16:9", "4:3", "16:10" })
        {
            var choice = CreateButton(ratio, ratio == "16:9" ? _brand : Color.White, ratio == "16:9" ? Color.White : Color.FromArgb(31, 48, 68), 52, 32);
            choice.Click += (_, _) =>
            {
                _theme.AspectRatio = ratio;
                foreach (Control control in ratioGroup.Controls)
                {
                    if (control is Button button)
                    {
                        var selected = button.Text == ratio;
                        button.BackColor = selected ? _brand : Color.White;
                        button.ForeColor = selected ? Color.White : Color.FromArgb(31, 48, 68);
                    }
                }
                RefreshSlides();
            };
            ratioGroup.Controls.Add(choice);
        }

        ribbon.Controls.AddRange([colorGroup, imageGroup, brightnessGroup, ratioGroup]);
        return ribbon;
    }

    private Control BuildWorkspace()
    {
        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(10, 10, 10, 12), BackColor = Color.FromArgb(241, 244, 247) };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.Controls.Add(BuildAgendaPanel(), 0, 0);
        workspace.Controls.Add(BuildEditorPanel(), 1, 0);
        workspace.Controls.Add(BuildPreviewPanel(), 2, 0);
        return workspace;
    }

    private Control BuildAgendaPanel()
    {
        var outer = CreateSection("Service agenda", "Songs selected for this service");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 5;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 52));

        var addButton = CreateButton("+  Add current song to agenda", _brand, Color.White, 185, 32);
        addButton.Click += (_, _) => AddCurrentSongToAgenda();
        _agendaList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 7, 0, 10) };
        _agendaList.SelectedIndexChanged += (_, _) => { if (_agendaList.SelectedIndex >= 0) _slideStatus.Text = "Agenda item selected · " + _agendaList.SelectedItem; };
        var libraryTitle = new Label { Text = "SONG LIBRARY", ForeColor = Color.FromArgb(77, 93, 111), Font = new Font("Segoe UI", 8F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
        var search = new TextBox { PlaceholderText = "Search songs", Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 6) };
        search.TextChanged += (_, _) => FilterLibrary(search.Text);
        _libraryList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F) };
        _libraryList.DoubleClick += (_, _) => LoadSelectedLibrarySong();
        content.Controls.Add(addButton, 0, 0);
        content.Controls.Add(_agendaList, 0, 1);
        content.Controls.Add(libraryTitle, 0, 2);
        content.Controls.Add(search, 0, 3);
        content.Controls.Add(_libraryList, 0, 4);
        FilterLibrary("");
        return outer;
    }

    private Control BuildEditorPanel()
    {
        var outer = CreateSection("Song editor", "Edit lyrics — slides update as you type");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 6;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 135));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        content.Controls.Add(SmallLabel("Song title"), 0, 0);
        _titleBox = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) };
        _titleBox.TextChanged += (_, _) => _slideStatus.Text = "Editing · " + (_titleBox.Text.Length == 0 ? "Untitled song" : _titleBox.Text);
        content.Controls.Add(_titleBox, 0, 1);
        _lyricsBox = new RichTextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12F), AcceptsTab = true, Margin = new Padding(0, 0, 0, 8) };
        _lyricsBox.TextChanged += (_, _) => { if (!_updating) RebuildSlides(); };
        content.Controls.Add(_lyricsBox, 0, 2);
        content.Controls.Add(SmallLabel("SLIDES"), 0, 3);
        _slideList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, 0, 0, 6) };
        _slideList.SelectedIndexChanged += (_, _) => { if (!_updating && _slideList.SelectedIndex >= 0) SelectSlide(_slideList.SelectedIndex); };
        content.Controls.Add(_slideList, 0, 4);
        _slideStatus = new Label { ForeColor = Color.FromArgb(88, 103, 120), AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
        content.Controls.Add(_slideStatus, 0, 5);
        return outer;
    }

    private Control BuildPreviewPanel()
    {
        var outer = CreateSection("Live slide", "What your congregation will see");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 4;
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _audiencePreview = new SlideCanvas { Dock = DockStyle.Fill, Theme = _theme, Margin = new Padding(0, 0, 0, 9), BackColor = Color.FromArgb(22, 28, 37) };
        var current = new Label { Text = "PREVIEW", ForeColor = Color.FromArgb(77, 93, 111), Font = new Font("Segoe UI", 8F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 2, 0, 6) };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, WrapContents = false, Margin = new Padding(0) };
        var previous = CreateButton("‹  Previous", Color.White, Color.FromArgb(31, 48, 68), 92, 33);
        previous.Click += (_, _) => SelectSlide(_currentSlide - 1);
        var live = CreateButton("●  Go live", Color.FromArgb(35, 157, 87), Color.White, 86, 33);
        live.Click += (_, _) => ToggleProjector();
        var next = CreateButton("Next  ›", _brand, Color.White, 78, 33);
        next.Click += (_, _) => SelectSlide(_currentSlide + 1);
        buttons.Controls.AddRange([previous, live, next]);
        var help = new Label { Text = "Tip: use ↑ / ↓ to change slides", ForeColor = Color.FromArgb(112, 125, 138), AutoSize = true, Margin = new Padding(0, 7, 0, 0) };
        content.Controls.Add(_audiencePreview, 0, 0);
        content.Controls.Add(current, 0, 1);
        content.Controls.Add(buttons, 0, 2);
        content.Controls.Add(help, 0, 3);
        return outer;
    }

    private Panel CreateSection(string title, string subtitle)
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

    private FlowLayoutPanel CreateRibbonGroup(string title)
    {
        var outer = new Panel { Width = 275, Height = 130, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(9, 8, 9, 22), BackColor = Color.White };
        outer.Paint += (_, e) =>
        {
            using var pen = new Pen(_panelBorder);
            e.Graphics.DrawLine(pen, outer.Width - 1, 4, outer.Width - 1, outer.Height - 4);
        };
        var label = new Label { Text = title, ForeColor = Color.FromArgb(91, 105, 121), AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, Location = new Point(10, 107) };
        var items = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoSize = false, BackColor = Color.White };
        outer.Controls.Add(items);
        outer.Controls.Add(label);
        items.Tag = outer;
        return items;
    }

    private static Control MakeField(string label, Control input)
    {
        var panel = new FlowLayoutPanel { Width = 122, Height = 52, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 9, 0) };
        panel.Controls.Add(new Label { Text = label, ForeColor = Color.FromArgb(86, 99, 114), AutoSize = true });
        panel.Controls.Add(input);
        return panel;
    }

    private static Label HintLabel(string text) => new() { Text = text, ForeColor = Color.FromArgb(112, 125, 138), AutoSize = true, MaximumSize = new Size(220, 0), Margin = new Padding(8, 7, 0, 0) };
    private static Label SmallLabel(string text) => new() { Text = text, ForeColor = Color.FromArgb(77, 93, 111), Font = new Font("Segoe UI", 8F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 4) };

    private Button CreateTabButton(string text, bool active)
    {
        var button = new Button { Text = text, Width = 112, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, BackColor = active ? Color.White : _darkBrand, ForeColor = active ? _darkBrand : Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
        return button;
    }

    private static Button CreateButton(string text, Color background, Color foreground, int width, int height)
    {
        var button = new Button { Text = text, Width = width, Height = height, BackColor = background, ForeColor = foreground, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderColor = Color.FromArgb(196, 206, 216), BorderSize = 1 }, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(0, 0, 6, 0), Cursor = Cursors.Hand, UseVisualStyleBackColor = false };
        return button;
    }

    private Button CreatePresetButton(string text, Color background, Color foreground, Action onClick)
    {
        var button = CreateButton(text, background, foreground, 79, 47);
        button.Click += (_, _) => onClick();
        return button;
    }

    private void LoadSong(Song song)
    {
        _updating = true;
        _titleBox.Text = song.Title;
        _lyricsBox.Text = song.Lyrics;
        _updating = false;
        RebuildSlides();
        _slideStatus.Text = "Loaded from Song Library · " + song.Title;
    }

    private void LoadSelectedLibrarySong()
    {
        if (_libraryList.SelectedItem is Song song) LoadSong(song);
    }

    private void FilterLibrary(string text)
    {
        if (_libraryList is null) return;
        _libraryList.BeginUpdate();
        _libraryList.Items.Clear();
        foreach (var song in _library.Where(s => s.Title.Contains(text, StringComparison.OrdinalIgnoreCase))) _libraryList.Items.Add(song);
        _libraryList.EndUpdate();
    }

    private void AddCurrentSongToAgenda()
    {
        var name = string.IsNullOrWhiteSpace(_titleBox.Text) ? "Untitled song" : _titleBox.Text.Trim();
        var item = $"{name}  ·  {_slides.Count} slides";
        _agenda.Add(item);
        _agendaList.Items.Add(item);
        _agendaList.SelectedIndex = _agendaList.Items.Count - 1;
    }

    private void RebuildSlides()
    {
        if (_lyricsBox is null) return;
        var maxLines = (int)_maxLines.Value;
        var chunks = _lyricsBox.Text.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _slides.Clear();
        foreach (var chunk in chunks)
        {
            var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var start = 0; start < lines.Length; start += maxLines)
            {
                _slides.Add(string.Join(Environment.NewLine, lines.Skip(start).Take(maxLines)));
            }
        }
        if (_slides.Count == 0) _slides.Add("Type song lyrics here");

        _updating = true;
        _slideList.Items.Clear();
        for (var index = 0; index < _slides.Count; index++)
        {
            var summary = _slides[index].Replace(Environment.NewLine, "  /  ");
            _slideList.Items.Add($"{index + 1:00}   {summary}");
        }
        _currentSlide = Math.Clamp(_currentSlide, 0, _slides.Count - 1);
        _slideList.SelectedIndex = _currentSlide;
        _updating = false;
        RefreshSlides();
    }

    private void SelectSlide(int requested)
    {
        if (_slides.Count == 0) return;
        _currentSlide = Math.Clamp(requested, 0, _slides.Count - 1);
        _updating = true;
        _slideList.SelectedIndex = _currentSlide;
        _updating = false;
        RefreshSlides();
    }

    private void RefreshSlides()
    {
        if (_slides.Count == 0 || _audiencePreview is null) return;
        _audiencePreview.SlideText = _slides[_currentSlide];
        _audiencePreview.Invalidate();
        _slideStatus.Text = $"Slide {_currentSlide + 1} of {_slides.Count} · {_theme.AspectRatio} · {_theme.FontFamily}";
        _projector?.SetSlide(_slides[_currentSlide], _theme);
    }

    private void SetTextStyle(Color color, bool bold)
    {
        _theme.TextColor = color;
        _theme.Bold = bold;
        _fontColorButton.BackColor = color;
        UpdateBoldButton();
        RefreshSlides();
    }

    private void UpdateBoldButton()
    {
        _boldButton.BackColor = _theme.Bold ? _brand : Color.FromArgb(232, 237, 244);
        _boldButton.ForeColor = _theme.Bold ? Color.White : Color.FromArgb(31, 48, 68);
    }

    private void ChooseTextColor()
    {
        using var dialog = new ColorDialog { Color = _theme.TextColor, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _theme.TextColor = dialog.Color;
        _fontColorButton.BackColor = dialog.Color;
        RefreshSlides();
    }

    private void ChooseBackgroundColor()
    {
        using var dialog = new ColorDialog { Color = _theme.BackgroundColor, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _theme.BackgroundColor = dialog.Color;
        _theme.BackgroundImage = null;
        RefreshSlides();
    }

    private void ChooseBackgroundImage()
    {
        using var dialog = new OpenFileDialog { Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*", Title = "Choose slide background" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            using var source = Image.FromFile(dialog.FileName);
            var replacement = new Bitmap(source);
            _theme.BackgroundImage?.Dispose();
            _theme.BackgroundImage = replacement;
            RefreshSlides();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, "The selected image could not be used.\n\n" + exception.Message, "Church Projector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ToggleProjector()
    {
        if (_projector is { IsDisposed: false })
        {
            _projector.Close();
            _projector = null;
            return;
        }

        _projector = new ProjectorForm();
        _projector.FormClosed += (_, _) => _projector = null;
        var targetScreen = Screen.AllScreens.Length > 1 ? Screen.AllScreens[1] : Screen.PrimaryScreen!;
        _projector.StartPosition = FormStartPosition.Manual;
        _projector.Bounds = targetScreen.Bounds;
        _projector.Show();
        RefreshSlides();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Down || keyData == Keys.PageDown || keyData == Keys.Right)
        {
            SelectSlide(_currentSlide + 1);
            return true;
        }
        if (keyData == Keys.Up || keyData == Keys.PageUp || keyData == Keys.Left)
        {
            SelectSlide(_currentSlide - 1);
            return true;
        }
        if (keyData == Keys.F5)
        {
            ToggleProjector();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _theme.BackgroundImage?.Dispose();
        base.Dispose(disposing);
    }
}

public sealed record Song(string Title, string Lyrics)
{
    public override string ToString() => Title;
}
