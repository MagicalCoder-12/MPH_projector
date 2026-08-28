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
    private ComboBox _librarySort = null!;
    private string _librarySortMode = "alphabetical";
    private SongApiClient? _apiClient;

    private RichTextBox _lyricsBox = null!;
    private TextBox _titleBox = null!;
    private ListBox _slideList = null!;
    private ListBox _agendaList = null!;
    private ListBox _libraryList = null!;
    private Label _slideStatus = null!;
    private SlideCanvas _audiencePreview = null!;
    private ComboBox _fontFamily = null!;
    private ComboBox _fontSize = null!;
    private Button _boldButton = null!;
    private Button _italicButton = null!;
    private Button _underlineButton = null!;
    private Button _strikethroughButton = null!;
    private Button _subscriptButton = null!;
    private Button _superscriptButton = null!;
    private ColorButton _fontColorButton = null!;
    private ColorButton _highlightColorButton = null!;
    private Button _clearFormattingButton = null!;
    private Button _cutButton = null!;
    private Button _copyButton = null!;
    private Button _pasteButton = null!;
    private Button _bulletsButton = null!;
    private Button _numberingButton = null!;
    private Button _decreaseIndentButton = null!;
    private Button _increaseIndentButton = null!;
    private ComboBox _lineSpacing = null!;
    private NumericUpDown _maxLines = null!;
    private RibbonIconButton _alignLeftButton = null!;
    private RibbonIconButton _alignCenterButton = null!;
    private RibbonIconButton _alignRightButton = null!;
    private RibbonIconButton _alignJustifyButton = null!;
    private TrackBar _brightness = null!;
    private BackgroundGalleryPanel _imageGallery = null!;
    private BackgroundGalleryPanel _videoGallery = null!;
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

    private Panel _ribbonHost = null!;
    private GalleryDropdownForm? _activeDropdown;
    private FileSystemWatcher? _imageWatcher;
    private FileSystemWatcher? _videoWatcher;
    private readonly System.Windows.Forms.Timer _bgWatcherDebounce = new() { Interval = 600 };
    private readonly System.Windows.Forms.Timer _bgPollTimer = new() { Interval = 3000 };
    private bool _bgWatcherPending;
    private readonly Color _brand = Color.FromArgb(65, 105, 225);
    private readonly Color _darkBrand = Color.FromArgb(49, 84, 179);
    private readonly Color _panelBorder = Color.FromArgb(217, 222, 231);

    private readonly System.Windows.Forms.Timer _syncTimer = new();
    private ToolStripStatusLabel? _statusSync;
    private bool _syncing;
    private bool _lastSyncFailed;
    private string? _syncError;

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
        foreach (var s in _data.Songs)
        {
            if (string.IsNullOrWhiteSpace(s.Owner)) s.Owner = "app";
            if (s.Tags is null || s.Tags.Count == 0) s.Tags = ["church"];
            else if (!s.Tags.Contains("church", StringComparer.OrdinalIgnoreCase)) s.Tags.Add("church");
            if (s.CreatedAt == default) s.CreatedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(0, 10000));
            if (s.UpdatedAt == default) s.UpdatedAt = s.CreatedAt;
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
        SetupBackgroundWatchers();
        _ = LoadSongsFromApiAsync();
        StartSyncTimer();
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
        try { _bibles.Add(_store.ImportSqliteBible(teluguBiblePath)); Persist(); }
        catch { }
    }

    private void EnsureDefaultLogo()
    {
        if (!string.IsNullOrWhiteSpace(_data.BackgroundPreferences.LogoPath) && File.Exists(_data.BackgroundPreferences.LogoPath)) return;
        var logoPath = _store.EnsureDefaultLogo();
        if (string.IsNullOrEmpty(logoPath)) return;
        _data.BackgroundPreferences.LogoPath = logoPath;
        try { _logoImage = Image.FromFile(logoPath); } catch { _logoImage = null; }
        Persist();
    }

    private void StartSyncTimer()
    {
        _syncTimer.Interval = 60_000;
        _syncTimer.Tick += async (_, _) => await SyncFromApiAsync();
        _syncTimer.Start();
    }

    private async Task SyncFromApiAsync()
    {
        if (_syncing) return;
        if (_apiClient is null)
        {
            try { _apiClient = new SongApiClient(); }
            catch { return; }
        }
        _syncing = true;
        try
        {
            // Pull from web
            var remoteSongs = await _apiClient.GetAllSongsAsync();
            var byRemoteId = remoteSongs.Where(s => !string.IsNullOrEmpty(s.Id)).ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
            var changed = false;
            var pulled = 0; var merged = 0; var updated = 0;
            foreach (var remote in byRemoteId.Values)
            {
                var existing = _library.FirstOrDefault(s => s.SourceId == remote.Id);
                if (existing is null)
                {
                    // Try title merge for local-only songs
                    existing = _library.FirstOrDefault(s => s.SourceId is null && string.Equals(s.Title, remote.Title, StringComparison.OrdinalIgnoreCase));
                    if (existing is not null) { existing.SourceId = remote.Id; merged++; changed = true; }
                }
                if (existing is null)
                {
                    _library.Add(new Song { SourceId = remote.Id, Title = remote.Title, Lyrics = remote.Lyrics, Owner = string.IsNullOrWhiteSpace(remote.Owner) ? "web" : remote.Owner, Tags = remote.Tags.Count > 0 ? remote.Tags : ["web"], CreatedAt = remote.CreatedAt == default ? DateTime.UtcNow : remote.CreatedAt, UpdatedAt = remote.UpdatedAt == default ? DateTime.UtcNow : remote.UpdatedAt });
                    pulled++; changed = true;
                }
                else if (string.Equals(remote.Source, "web", StringComparison.OrdinalIgnoreCase))
                {
                    var newOwner = string.IsNullOrWhiteSpace(remote.Owner) ? "web" : remote.Owner;
                    if (!string.Equals(existing.Lyrics, remote.Lyrics, StringComparison.Ordinal) || !string.Equals(existing.Title, remote.Title, StringComparison.Ordinal) || existing.Owner != newOwner)
                    { existing.Title = remote.Title; existing.Lyrics = remote.Lyrics; existing.Owner = newOwner; existing.Tags = remote.Tags.Count > 0 ? remote.Tags : existing.Tags; existing.UpdatedAt = remote.UpdatedAt == default ? DateTime.UtcNow : remote.UpdatedAt; updated++; changed = true; }
                }
            }
            if (changed) Persist();
            FilterLibrary(_librarySearch?.Text ?? string.Empty);

            // Push to web
            var (pushCount, linkCount, pushUpdates) = await PushLocalSongsToApiAsync();

            _lastSyncFailed = false;
            UpdateSyncStatus(true, pulled, merged, updated, pushCount, linkCount);
        }
        catch (Exception ex) { _lastSyncFailed = true; _syncError = ex.Message; UpdateSyncStatus(false); }
        finally { _syncing = false; }
    }

    private async Task<(int pushed, int linked, int pushedUpdates)> PushLocalSongsToApiAsync()
    {
        if (_apiClient is null) return (0, 0, 0);
        List<ApiSong> remoteSongs;
        try { remoteSongs = await _apiClient.GetAllSongsAsync(); }
        catch { return (0, 0, 0); }
        var remoteByTitle = remoteSongs.Where(s => !string.IsNullOrWhiteSpace(s.Title))
            .ToDictionary(s => s.Title.Trim(), s => s, StringComparer.OrdinalIgnoreCase);
        var remoteById = remoteSongs.Where(s => !string.IsNullOrEmpty(s.Id))
            .ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
        var pushed = 0; var linked = 0; var pushedUpdates = 0; var changed = false;

        // Push updates for desktop-owned songs
        foreach (var song in _library.Where(s => s.SourceId is not null && !string.IsNullOrWhiteSpace(s.Title)).ToList())
        {
            if (!remoteById.TryGetValue(song.SourceId!, out var remote) || !string.Equals(remote.Source, "desktop", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(song.Lyrics, remote.Lyrics, StringComparison.Ordinal)) continue;
            try
            {
                var api = ToApiSong(song); api.Tags = remote.Tags.Count > 0 ? remote.Tags : ["church"]; api.Desktop = "true"; api.Source = "desktop";
                await _apiClient.UpdateSongAsync(song.SourceId!, api);
                pushedUpdates++; changed = true;
            }
            catch { }
        }

        // Push new local songs
        foreach (var song in _library.Where(s => s.SourceId is null && !string.IsNullOrWhiteSpace(s.Title)).ToList())
        {
            try
            {
                if (remoteByTitle.TryGetValue(song.Title.Trim(), out var remote))
                {
                    song.SourceId = remote.Id; linked++; changed = true; continue;
                }
                var api = ToApiSong(song); api.Tags = ["church"]; api.Desktop = "true"; api.Source = "desktop";
                var created = await _apiClient.CreateSongAsync(api);
                if (created is not null) song.SourceId = created.Id;
                pushed++; changed = true;
            }
            catch { }
        }
        if (changed) { Persist(); FilterLibrary(_librarySearch?.Text ?? string.Empty); }
        return (pushed, linked, pushedUpdates);
    }

    private async Task LoadSongsFromApiAsync()
    {
        if (_apiClient is not null) return;
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _apiClient = new SongApiClient();
                var remoteSongs = await _apiClient.GetAllSongsAsync();
                var byRemoteId = remoteSongs.Where(s => !string.IsNullOrEmpty(s.Id)).ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
                foreach (var remote in byRemoteId.Values)
                {
                    var existing = _library.FirstOrDefault(s => s.SourceId == remote.Id);
                    if (existing is null)
                    {
                        existing = _library.FirstOrDefault(s => s.SourceId is null && string.Equals(s.Title, remote.Title, StringComparison.OrdinalIgnoreCase));
                        if (existing is not null) existing.SourceId = remote.Id;
                    }
                    if (existing is null)
                        _library.Add(new Song { SourceId = remote.Id, Title = remote.Title, Lyrics = remote.Lyrics, Owner = string.IsNullOrWhiteSpace(remote.Owner) ? "web" : remote.Owner, Tags = remote.Tags.Count > 0 ? remote.Tags : ["web"], CreatedAt = remote.CreatedAt == default ? DateTime.UtcNow : remote.CreatedAt, UpdatedAt = remote.UpdatedAt == default ? DateTime.UtcNow : remote.UpdatedAt });
                    else if (string.Equals(remote.Source, "web", StringComparison.OrdinalIgnoreCase))
                    {
                        existing.Title = remote.Title; existing.Lyrics = remote.Lyrics; existing.Owner = string.IsNullOrWhiteSpace(remote.Owner) ? "web" : remote.Owner; existing.Tags = remote.Tags.Count > 0 ? remote.Tags : existing.Tags; existing.UpdatedAt = remote.UpdatedAt == default ? DateTime.UtcNow : remote.UpdatedAt;
                    }
                }
                Persist(); FilterLibrary(_librarySearch?.Text ?? string.Empty); _lastSyncFailed = false; UpdateSyncStatus(true);
                _ = PushLocalSongsToApiAsync(); return;
            }
            catch { _apiClient = null; if (attempt < maxRetries) await Task.Delay(1000 * attempt); }
        }
        _lastSyncFailed = true; UpdateSyncStatus(false);
    }

    private void UpdateSyncStatus(bool success, int pulled = 0, int merged = 0, int updated = 0, int pushed = 0, int linked = 0)
    {
        if (_statusSync is null) return;
        if (success)
        {
            var parts = new List<string>();
            if (pulled > 0) parts.Add($"+{pulled} pulled"); if (merged > 0) parts.Add($"{merged} merged"); if (updated > 0) parts.Add($"{updated} updated"); if (pushed > 0) parts.Add($"+{pushed} pushed"); if (linked > 0) parts.Add($"{linked} linked");
            _statusSync.Text = parts.Count > 0 ? $"\u25cf Synced ({string.Join(", ", parts)})" : "\u25cf Synced";
            _statusSync.ForeColor = Color.FromArgb(150, 230, 180);
        }
        else { _statusSync.Text = "\u25cb Offline"; _statusSync.ForeColor = Color.FromArgb(255, 180, 120); _statusSync.ToolTipText = _syncError ?? ""; }
    }

    private static ApiSong ToApiSong(Song song) => new()
    {
        Title = song.Title,
        Lyrics = song.Lyrics,
        SongLanguage = "Other",
        Tags = song.Tags.Contains("church", StringComparer.OrdinalIgnoreCase) ? song.Tags : [.. song.Tags, "church"],
        Owner = string.IsNullOrWhiteSpace(song.Owner) ? "app" : song.Owner,
        Source = "desktop",
        Desktop = "true",
        CreatedAt = song.CreatedAt,
        UpdatedAt = DateTime.UtcNow,
    };

    private void LoadSong(Song song) => LoadSongContent(song.Title, song.Lyrics, song.Id, "Loaded from Song Library");
    private void LoadSongContent(string title, string lyrics, Guid? songId, string source)
    {
        _updating = true; _titleBox.Text = title; _lyricsBox.Text = lyrics; _currentSongId = songId; _updating = false;
        RebuildSlides(); _slideStatus.Text = source + " - " + title;
    }
    private void FilterLibrary(string text)
    {
        if (_libraryList is null) return;
        _libraryList.BeginUpdate();
        _libraryList.Items.Clear();
        var query = _library.Where(s => string.IsNullOrWhiteSpace(text)
            || s.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
            || s.Lyrics.Contains(text, StringComparison.OrdinalIgnoreCase));
        IEnumerable<Song> sorted = _librarySortMode switch
        {
            "recent" => query.OrderByDescending(s => s.CreatedAt),
            "oldest" => query.OrderBy(s => s.CreatedAt),
            _ => query.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
        };
        foreach (var song in sorted) _libraryList.Items.Add(song);
        _libraryList.EndUpdate();
    }
    private void AddCurrentSongToAgenda()
    {
        var name = string.IsNullOrWhiteSpace(_titleBox.Text) ? "Untitled song" : _titleBox.Text.Trim();
        _agenda.Add(new AgendaItem { SongId = _currentSongId, Title = name, LyricsSnapshot = _lyricsBox.Text });
        Persist(); RefreshAgenda(); _agendaList.SelectedIndex = _agenda.Count - 1;
    }
    private void RebuildSlides()
    {
        if (_lyricsBox is null) return;
        var maxLines = (int)_maxLines.Value;
        var chunks = _lyricsBox.Text.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _slides.Clear(); _verseSlideIndexes.Clear();
        foreach (var chunk in chunks) { _verseSlideIndexes.Add(_slides.Count); var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); for (var start = 0; start < lines.Length; start += maxLines) _slides.Add(string.Join(Environment.NewLine, lines.Skip(start).Take(maxLines))); }
        if (_slides.Count == 0) { _slides.Add("Type song lyrics here"); _verseSlideIndexes.Add(0); }
        _updating = true; _slideList.Items.Clear();
        for (var index = 0; index < _slides.Count; index++) { var summary = _slides[index].Replace(Environment.NewLine, "  /  "); _slideList.Items.Add($"V{index + 1}   {summary}"); }
        _currentSlide = Math.Clamp(_currentSlide, 0, _slides.Count - 1); _slideList.SelectedIndex = _currentSlide; _updating = false; RefreshSlides();
    }
    private void SelectSlide(int requested) { if (_slides.Count == 0) return; _currentSlide = Math.Clamp(requested, 0, _slides.Count - 1); _updating = true; _slideList.SelectedIndex = _currentSlide; _updating = false; RefreshSlides(); }
    private void RefreshSlides()
    {
        if (_slides.Count == 0) return;
        if (_audiencePreview is not null) { _audiencePreview.SlideText = _slides[_currentSlide]; _audiencePreview.Invalidate(); }
        if (_biblePreview is not null) { _biblePreview.SlideText = _slides[_currentSlide]; _biblePreview.Invalidate(); }
        if (_slideStatus is not null) _slideStatus.Text = $"Slide {_currentSlide + 1} of {_slides.Count} - {_theme.AspectRatio} - {_theme.FontFamily}";
        if (_statusSlide is not null) _statusSlide.Text = $"Slide {_currentSlide + 1} of {_slides.Count}";
        _projector?.SetSlide(_slides[_currentSlide], _theme); _videoProjector?.SetSlide(_slides[_currentSlide], _theme);
    }
    private void SetTextStyle(Color color, bool bold) { _theme.TextColor = color; _theme.Bold = bold; _fontColorButton.Swatch = color; UpdateBoldButton(); RefreshSlides(); }
    private static void SetRibbonToggleState(Button button, bool on)
    {
        button.Tag = on;
        button.BackColor = on ? RibbonIconButton.ActiveBack : RibbonIconButton.IdleBack;
        button.ForeColor = on ? RibbonIconButton.ActiveInk : RibbonIconButton.IdleInk;
        button.FlatAppearance.BorderColor = on ? RibbonIconButton.ActiveBorder : RibbonIconButton.IdleBorder;
    }
    private void UpdateBoldButton() => SetRibbonToggleState(_boldButton, _theme.Bold);
    private void UpdateItalicButton() => SetRibbonToggleState(_italicButton, _theme.Italic);
    private void UpdateUnderlineButton() => SetRibbonToggleState(_underlineButton, _theme.Underline);
    private void UpdateStrikethroughButton() => SetRibbonToggleState(_strikethroughButton, _theme.Strikethrough);
    private void UpdateSubSuperButtons()
    {
        SetRibbonToggleState(_subscriptButton, _theme.Subscript);
        SetRibbonToggleState(_superscriptButton, _theme.Superscript);
    }
    private void ChooseHighlightColor() { using var dialog = new ColorDialog { Color = _theme.HighlightColor == Color.Transparent ? Color.FromArgb(255, 255, 153) : _theme.HighlightColor, FullOpen = true }; if (dialog.ShowDialog(this) != DialogResult.OK) return; _theme.HighlightColor = dialog.Color; _highlightColorButton.Swatch = dialog.Color; RefreshSlides(); }
    private void ClearFormatting() { _theme.Bold = false; _theme.Italic = false; _theme.Underline = false; _theme.Strikethrough = false; _theme.Subscript = false; _theme.Superscript = false; _theme.TextColor = Color.Black; UpdateBoldButton(); UpdateItalicButton(); UpdateUnderlineButton(); UpdateStrikethroughButton(); UpdateSubSuperButtons(); _fontColorButton.Swatch = Color.Black; RefreshSlides(); }
    private void ToggleBullets() { if (_lyricsBox.SelectionLength == 0) return; var start = _lyricsBox.SelectionStart; var text = _lyricsBox.Text; var lineStart = text.LastIndexOf('\n', start - 1) + 1; var lineEnd = text.IndexOf('\n', start); if (lineEnd < 0) lineEnd = text.Length; var line = text.Substring(lineStart, lineEnd - lineStart); if (line.TrimStart().StartsWith("• ")) _lyricsBox.Text = text.Substring(0, lineStart) + line.Replace("• ", "", StringComparison.Ordinal) + text.Substring(lineEnd); else _lyricsBox.Text = text.Substring(0, lineStart) + "• " + line.TrimStart() + text.Substring(lineEnd); _lyricsBox.SelectionStart = start; RebuildSlides(); }
    private void ToggleNumbering() { if (_lyricsBox.SelectionLength == 0) return; var start = _lyricsBox.SelectionStart; var text = _lyricsBox.Text; var lineStart = text.LastIndexOf('\n', start - 1) + 1; var lineEnd = text.IndexOf('\n', start); if (lineEnd < 0) lineEnd = text.Length; var line = text.Substring(lineStart, lineEnd - lineStart); if (System.Text.RegularExpressions.Regex.IsMatch(line.TrimStart(), @"^\d+\.\s")) _lyricsBox.Text = text.Substring(0, lineStart) + System.Text.RegularExpressions.Regex.Replace(line.TrimStart(), @"^\d+\.\s", "") + text.Substring(lineEnd); else _lyricsBox.Text = text.Substring(0, lineStart) + "1. " + line.TrimStart() + text.Substring(lineEnd); _lyricsBox.SelectionStart = start; RebuildSlides(); }
    private void AdjustIndent(int delta) { if (_lyricsBox.SelectionLength == 0) return; }
    private void UpdateProjectorStatus()
    {
        if (_statusProjector is null) return;
        var live = (_projector is { IsDisposed: false }) || _videoProjector is not null;
        _statusProjector.Text = live ? "\u25cf Projector: live" : "Projector: off";
        _statusProjector.ForeColor = live ? Color.FromArgb(150, 230, 180) : Color.White;
        if (_projectorButton is not null) { _projectorButton.Text = live ? "\u25a3  Close projector" : "\u25a3  Open projector"; _projectorButton.BackColor = live ? Color.FromArgb(35, 157, 87) : Color.FromArgb(37, 66, 143); }
    }
    private void SetStageMode(StageMode mode) { _stageMode = _stageMode == mode ? StageMode.Slide : mode; if (_stageMode == StageMode.Logo && _logoImage is null) { SetLogoPath(); if (_logoImage is null) _stageMode = StageMode.Slide; } ApplyStageToProjectors(); UpdateStageStatus(); }
    private void ApplyStageToProjectors()
    {
        _projector?.SetStage(_stageMode, _logoImage);
        _videoProjector?.SetStage(_stageMode, _logoImage);
        if (_audiencePreview is not null) { _audiencePreview.Stage = _stageMode; _audiencePreview.LogoImage = _logoImage; _audiencePreview.Invalidate(); }
        if (_biblePreview is not null) { _biblePreview.Stage = _stageMode; _biblePreview.LogoImage = _logoImage; _biblePreview.Invalidate(); }
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
        var detail = _stageMode switch { StageMode.Black => "black screen", StageMode.Background => "background only", StageMode.Logo => "logo", _ => "live" };
        _statusProjector.Text = $"\u25cf Projector: {detail}";
    }
    private static void SetStageButton(Button button, bool active) { button.BackColor = active ? Color.FromArgb(35, 157, 87) : Color.FromArgb(243, 246, 255); button.ForeColor = active ? Color.White : Color.FromArgb(52, 64, 84); }
    private void SetLogoPath() { using var dialog = new OpenFileDialog { Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*", Title = "Choose church logo" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; var path = _store.ImportLogo(dialog.FileName); if (string.IsNullOrEmpty(path)) return; _data.BackgroundPreferences.LogoPath = path; _logoImage?.Dispose(); try { _logoImage = Image.FromFile(path); } catch { _logoImage = null; } Persist(); }
    private void LoadLogoImage() { _logoImage?.Dispose(); _logoImage = null; var path = _data.BackgroundPreferences.LogoPath; if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return; try { _logoImage = Image.FromFile(path); } catch { _logoImage = null; } }
    private void ChooseTextColor() { using var dialog = new ColorDialog { Color = _theme.TextColor, FullOpen = true }; if (dialog.ShowDialog(this) != DialogResult.OK) return; _theme.TextColor = dialog.Color; _fontColorButton.Swatch = dialog.Color; RefreshSlides(); }
    private void ChooseBackgroundColor() { using var dialog = new ColorDialog { Color = _theme.BackgroundColor, FullOpen = true }; if (dialog.ShowDialog(this) != DialogResult.OK) return; _theme.BackgroundColor = dialog.Color; ClearBackgroundSelection(); SaveBackgroundPreferences(); RefreshSlides(); }
    private void ChooseBackgroundImage() { ImportBackground("Image"); }
    private void ToggleProjector()
    {
        if (_projector is { IsDisposed: false }) { _projector.Close(); _projector = null; UpdateProjectorStatus(); return; }
        if (_videoProjector is not null) { _videoProjector.Close(); _videoProjector = null; UpdateProjectorStatus(); return; }
        var screen = Screen.AllScreens.Length > 1 ? Screen.AllScreens[1] : Screen.PrimaryScreen!;
        if (!string.IsNullOrWhiteSpace(_theme.BackgroundVideoPath) && File.Exists(_theme.BackgroundVideoPath)) { _videoProjector = new VideoProjectorWindow(screen); _videoProjector.Closed += (_, _) => { _videoProjector = null; UpdateProjectorStatus(); }; _videoProjector.Show(); RefreshSlides(); UpdateProjectorStatus(); ApplyStageToProjectors(); return; }
        _projector = new ProjectorForm(); _projector.FormClosed += (_, _) => { _projector = null; UpdateProjectorStatus(); }; _projector.TargetScreen = screen; _projector.Show(); RefreshSlides(); UpdateProjectorStatus(); ApplyStageToProjectors();
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var verse = keyData switch { Keys.D1 or Keys.NumPad1 => 1, Keys.D2 or Keys.NumPad2 => 2, Keys.D3 or Keys.NumPad3 => 3, Keys.D4 or Keys.NumPad4 => 4, Keys.D5 or Keys.NumPad5 => 5, Keys.D6 or Keys.NumPad6 => 6, Keys.D7 or Keys.NumPad7 => 7, Keys.D8 or Keys.NumPad8 => 8, Keys.D9 or Keys.NumPad9 => 9, _ => 0 };
        if (_currentTab == "text" && verse > 0 && verse <= _verseSlideIndexes.Count) { SelectSlide(_verseSlideIndexes[verse - 1]); return true; }
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
            _clockTimer.Stop(); _clockTimer.Dispose(); _syncTimer.Stop(); _syncTimer.Dispose();
            _bgWatcherDebounce.Stop(); _bgWatcherDebounce.Dispose();
            _bgPollTimer.Stop(); _bgPollTimer.Dispose();
            _imageWatcher?.Dispose(); _videoWatcher?.Dispose();
            _theme.BackgroundImage?.Dispose(); _videoProjector?.Close(); _apiClient?.Dispose();
        }
        base.Dispose(disposing);
    }
}
