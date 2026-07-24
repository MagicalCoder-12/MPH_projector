namespace ChurchProjector;

public partial class Form1 : Form
{
    private readonly PresentationTheme _theme = new();
    private readonly LocalDataStore _store = new();
    private readonly AppData _data;
    private List<Song> _library = [];
    private List<AgendaItem> _agenda = [];
    private List<BibleTranslation> _bibles = [];
    private List<BackgroundAsset> _backgrounds = [];
    private ListBox? _activeAgendaList;
    private string _currentTab = "text";
    private readonly List<string> _slides = [];
    private readonly List<int> _verseSlideIndexes = [];
    private TextBox _librarySearch = null!;

    private RichTextBox _lyricsBox = null!;
    private TextBox _titleBox = null!;
    private ListBox _slideList = null!;
    private ListBox _agendaList = null!;
    private ListBox _libraryList = null!;
    private Label _slideStatus = null!;
    private SlideCanvas _audiencePreview = null!;
    private ComboBox _fontFamily = null!;
    private NumericUpDown _fontSize = null!;
    private CheckBox _autoFit = null!;
    private Button _boldButton = null!;
    private Button _fontColorButton = null!;
    private ComboBox _alignment = null!;
    private NumericUpDown _maxLines = null!;
    private TrackBar _brightness = null!;
    private ComboBox _backgroundPicker = null!;
    private CheckBox _videoLoop = null!;
    private Control _songWorkspace = null!;
    private Control _bibleWorkspace = null!;
    private Control _helpWorkspace = null!;
    private SlideCanvas _biblePreview = null!;
    private ListBox _bibleList = null!;
    private ListBox _bibleVerseList = null!;
    private ListBox _bibleAgendaList = null!;
    private ListBox _bibleBookList = null!;
    private ListBox _bibleChapterList = null!;
    private ComboBox _bibleTranslationPicker = null!;
    private TextBox _bibleReferenceBox = null!;
    private Label _bibleReferenceLabel = null!;
    private TextBox _bibleNameBox = null!;
    private TextBox _bibleBookBox = null!;
    private NumericUpDown _bibleChapter = null!;
    private NumericUpDown _bibleVerseNumber = null!;
    private RichTextBox _bibleVerseText = null!;
    private ProjectorForm? _projector;
    private VideoProjectorWindow? _videoProjector;
    private int _currentSlide;
    private Guid? _currentSongId;
    private Guid? _currentBibleId;
    private Guid? _currentBibleVerseId;
    private bool _updating;

    private StatusStrip _statusBar = null!;
    private ToolStripStatusLabel _statusTab = null!;
    private ToolStripStatusLabel _statusSlide = null!;
    private ToolStripStatusLabel _statusProjector = null!;
    private ToolStripStatusLabel _statusClock = null!;
    private Button _projectorButton = null!;
    private readonly System.Windows.Forms.Timer _clockTimer = new();

    private StageMode _stageMode = StageMode.Slide;
    private Image? _logoImage;
    private Button _blackButton = null!;
    private Button _hideTextButton = null!;
    private Button _logoButton = null!;

    private readonly Color _brand = Color.FromArgb(22, 113, 180);
    private readonly Color _darkBrand = Color.FromArgb(14, 83, 143);
    private readonly Color _panelBorder = Color.FromArgb(210, 218, 227);

    public Form1()
    {
        InitializeComponent();
        _data = _store.Load();
        EnsureDefaultLogo();
        if (_data.Songs.Count == 0)
        {
            _data.Songs.AddRange(DefaultSongs());
            _store.Save(_data);
        }
        _library = _data.Songs;
        _agenda = _data.Agenda;
        _bibles = _data.Bibles;
        ImportConfiguredTeluguBible();
        _backgrounds = _data.Backgrounds;
        SyncConfiguredBackgrounds();
        RestoreBackgroundPreferences();
        LoadLogoImage();
        BuildInterface();
        UpdateBoldButton();
        UpdateProjectorStatus();
        UpdateStageStatus();
        _clockTimer.Interval = 1000;
        _clockTimer.Tick += (_, _) => { if (_statusClock is not null) _statusClock.Text = DateTime.Now.ToShortTimeString(); };
        _clockTimer.Start();
        RefreshAgenda();
        if (_library.Count > 0) LoadSong(_library[0]);
    }

    private static IEnumerable<Song> DefaultSongs() =>
    [
        new("Great Is Your Faithfulness", "Great is Your faithfulness, O God my Father\nThere is no shadow of turning with You\n\nYou never change, You are compassionate\nAll that You are is forever true\n\nGreat is Your faithfulness\nGreat is Your faithfulness\nMorning by morning new mercies I see\n\nAll I have needed Your hand has provided\nGreat is Your faithfulness, Lord, unto me"),
        new("Way Maker", "You are here, moving in our midst\nI worship You, I worship You\n\nYou are here, working in this place\nI worship You, I worship You\n\nWay Maker, miracle worker\nPromise keeper, light in the darkness\nMy God, that is who You are"),
        new("Amazing Grace", "Amazing grace, how sweet the sound\nThat saved a wretch like me\n\nI once was lost, but now am found\nWas blind, but now I see")
    ];

    private void ImportConfiguredTeluguBible()
    {
        const string teluguBiblePath = @"C:\Users\ajith\AppData\Roaming\MPH_projector\Bible\telugu.db";
        if (!File.Exists(teluguBiblePath) || _bibles.Any(bible => string.Equals(bible.Name, "Telugu_Bible_BSI", StringComparison.OrdinalIgnoreCase))) return;
        try
        {
            _bibles.Add(_store.ImportSqliteBible(teluguBiblePath));
            Persist();
        }
        catch
        {
            // The Bible can still be imported manually through the Bible tab if the source file changes.
        }
    }

    private void EnsureDefaultLogo()
    {
        if (!string.IsNullOrWhiteSpace(_data.BackgroundPreferences.LogoPath) && File.Exists(_data.BackgroundPreferences.LogoPath)) return;
        var logoPath = _store.EnsureDefaultLogo();
        if (string.IsNullOrEmpty(logoPath)) return;
        _data.BackgroundPreferences.LogoPath = logoPath;
        try { _logoImage = Image.FromFile(logoPath); }
        catch { _logoImage = null; }
        Persist();
    }

    private void LoadSong(Song song)
        => LoadSongContent(song.Title, song.Lyrics, song.Id, "Loaded from Song Library");

    private void LoadSongContent(string title, string lyrics, Guid? songId, string source)
    {
        _updating = true;
        _titleBox.Text = title;
        _lyricsBox.Text = lyrics;
        _currentSongId = songId;
        _updating = false;
        RebuildSlides();
        _slideStatus.Text = source + " - " + title;
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
        _agenda.Add(new AgendaItem { SongId = _currentSongId, Title = name, LyricsSnapshot = _lyricsBox.Text });
        Persist();
        RefreshAgenda();
        _agendaList.SelectedIndex = _agenda.Count - 1;
    }

    private void RebuildSlides()
    {
        if (_lyricsBox is null) return;
        var maxLines = (int)_maxLines.Value;
        var chunks = _lyricsBox.Text.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _slides.Clear();
        _verseSlideIndexes.Clear();
        foreach (var chunk in chunks)
        {
            _verseSlideIndexes.Add(_slides.Count);
            var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var start = 0; start < lines.Length; start += maxLines)
                _slides.Add(string.Join(Environment.NewLine, lines.Skip(start).Take(maxLines)));
        }
        if (_slides.Count == 0)
        {
            _slides.Add("Type song lyrics here");
            _verseSlideIndexes.Add(0);
        }

        _updating = true;
        _slideList.Items.Clear();
        for (var index = 0; index < _slides.Count; index++)
        {
            var summary = _slides[index].Replace(Environment.NewLine, "  /  ");
            var verseIndex = _verseSlideIndexes.FindLastIndex(start => start <= index);
            var part = index == _verseSlideIndexes[verseIndex] ? "" : "b";
            _slideList.Items.Add($"V{verseIndex + 1}{part}   {summary}");
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
        if (_slides.Count == 0) return;
        if (_audiencePreview is not null)
        {
            _audiencePreview.SlideText = _slides[_currentSlide];
            _audiencePreview.Invalidate();
        }
        if (_biblePreview is not null)
        {
            _biblePreview.SlideText = _slides[_currentSlide];
            _biblePreview.Invalidate();
        }
        if (_slideStatus is not null) _slideStatus.Text = $"Slide {_currentSlide + 1} of {_slides.Count} - {_theme.AspectRatio} - {_theme.FontFamily}";
        if (_statusSlide is not null) _statusSlide.Text = $"Slide {_currentSlide + 1} of {_slides.Count}";
        _projector?.SetSlide(_slides[_currentSlide], _theme);
        _videoProjector?.SetSlide(_slides[_currentSlide], _theme);
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

    private void UpdateProjectorStatus()
    {
        if (_statusProjector is null) return;
        var live = (_projector is { IsDisposed: false }) || _videoProjector is not null;
        _statusProjector.Text = live ? "● Projector: live" : "Projector: off";
        _statusProjector.ForeColor = live ? Color.FromArgb(150, 230, 180) : Color.White;
        if (_projectorButton is not null)
        {
            _projectorButton.Text = live ? "▣  Close projector" : "▣  Open projector";
            _projectorButton.BackColor = live ? Color.FromArgb(35, 157, 87) : Color.FromArgb(11, 77, 132);
        }
    }

    private void SetStageMode(StageMode mode)
    {
        _stageMode = _stageMode == mode ? StageMode.Slide : mode;
        if (_stageMode == StageMode.Logo && _logoImage is null)
        {
            SetLogoPath();
            if (_logoImage is null) _stageMode = StageMode.Slide;
        }
        ApplyStageToProjectors();
        UpdateStageStatus();
    }

    private void ApplyStageToProjectors()
    {
        _projector?.SetStage(_stageMode, _logoImage);
        _videoProjector?.SetStage(_stageMode, _logoImage);
        UpdateStageStatus();
    }

    private void UpdateStageStatus()
    {
        if (_blackButton is not null) SetStageButton(_blackButton, _stageMode == StageMode.Black);
        if (_hideTextButton is not null) SetStageButton(_hideTextButton, _stageMode == StageMode.Background);
        if (_logoButton is not null) SetStageButton(_logoButton, _stageMode == StageMode.Logo);
        if (_statusProjector is null) return;
        var live = (_projector is { IsDisposed: false }) || _videoProjector is not null;
        if (!live) return;
        var detail = _stageMode switch
        {
            StageMode.Black => "black screen",
            StageMode.Background => "background only",
            StageMode.Logo => "logo",
            _ => "live"
        };
        _statusProjector.Text = $"● Projector: {detail}";
    }

    private static void SetStageButton(Button button, bool active)
    {
        button.BackColor = active ? Color.FromArgb(35, 157, 87) : Color.FromArgb(232, 237, 244);
        button.ForeColor = active ? Color.White : Color.FromArgb(31, 48, 68);
    }

    private void SetLogoPath()
    {
        using var dialog = new OpenFileDialog { Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*", Title = "Choose church logo" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var path = _store.ImportLogo(dialog.FileName);
        if (string.IsNullOrEmpty(path)) return;
        _data.BackgroundPreferences.LogoPath = path;
        _logoImage?.Dispose();
        try { _logoImage = Image.FromFile(path); }
        catch { _logoImage = null; }
        Persist();
    }

    private void LoadLogoImage()
    {
        _logoImage?.Dispose();
        _logoImage = null;
        var path = _data.BackgroundPreferences.LogoPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try { _logoImage = Image.FromFile(path); }
        catch { _logoImage = null; }
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
        ClearBackgroundSelection();
        SaveBackgroundPreferences();
        RefreshSlides();
    }

    private void ChooseBackgroundImage()
    {
        ImportBackground("Image");
    }

    private void ToggleProjector()
    {
        if (_projector is { IsDisposed: false })
        {
            _projector.Close();
            _projector = null;
            UpdateProjectorStatus();
            return;
        }
        if (_videoProjector is not null)
        {
            _videoProjector.Close();
            _videoProjector = null;
            UpdateProjectorStatus();
            return;
        }
        var screen = Screen.AllScreens.Length > 1 ? Screen.AllScreens[1] : Screen.PrimaryScreen!;
        if (!string.IsNullOrWhiteSpace(_theme.BackgroundVideoPath) && File.Exists(_theme.BackgroundVideoPath))
        {
            _videoProjector = new VideoProjectorWindow(screen);
            _videoProjector.Closed += (_, _) => { _videoProjector = null; UpdateProjectorStatus(); };
            _videoProjector.Show();
            RefreshSlides();
            UpdateProjectorStatus();
            ApplyStageToProjectors();
            return;
        }
        _projector = new ProjectorForm();
        _projector.FormClosed += (_, _) => { _projector = null; UpdateProjectorStatus(); };
        _projector.TargetScreen = screen;
        _projector.Show();
        RefreshSlides();
        UpdateProjectorStatus();
        ApplyStageToProjectors();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var verse = keyData switch
        {
            Keys.D1 or Keys.NumPad1 => 1, Keys.D2 or Keys.NumPad2 => 2, Keys.D3 or Keys.NumPad3 => 3,
            Keys.D4 or Keys.NumPad4 => 4, Keys.D5 or Keys.NumPad5 => 5, Keys.D6 or Keys.NumPad6 => 6,
            Keys.D7 or Keys.NumPad7 => 7, Keys.D8 or Keys.NumPad8 => 8, Keys.D9 or Keys.NumPad9 => 9,
            _ => 0
        };
        if (_currentTab == "text" && verse > 0 && verse <= _verseSlideIndexes.Count)
        {
            SelectSlide(_verseSlideIndexes[verse - 1]);
            return true;
        }
        if (keyData is Keys.Down or Keys.PageDown or Keys.Right) { SelectSlide(_currentSlide + 1); return true; }
        if (keyData is Keys.Up or Keys.PageUp or Keys.Left) { SelectSlide(_currentSlide - 1); return true; }
        if (keyData == Keys.F2) { SetStageMode(StageMode.Black); return true; }
        if (keyData == Keys.F3) { SetStageMode(StageMode.Background); return true; }
        if (keyData == Keys.F4) { SetStageMode(StageMode.Logo); return true; }
        if (keyData == Keys.F5) { ToggleProjector(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clockTimer.Stop();
            _clockTimer.Dispose();
            _theme.BackgroundImage?.Dispose();
            _videoProjector?.Close();
        }
        base.Dispose(disposing);
    }
}
