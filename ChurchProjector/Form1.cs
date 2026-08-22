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
    private MongoDbService? _mongoDb;

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
    private Button _italicButton = null!;
    private Button _underlineButton = null!;
    private Button _strikethroughButton = null!;
    private Button _subscriptButton = null!;
    private Button _superscriptButton = null!;
    private Button _fontColorButton = null!;
    private Button _highlightColorButton = null!;
    private Button _clearFormattingButton = null!;
    private Button _cutButton = null!;
    private Button _copyButton = null!;
    private Button _pasteButton = null!;
    private Button _bulletsButton = null!;
    private Button _numberingButton = null!;
    private Button _decreaseIndentButton = null!;
    private Button _increaseIndentButton = null!;
    private NumericUpDown _lineSpacing = null!;
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
        _library = _data.Songs;
        _agenda = _data.Agenda;
        _bibles = _data.Bibles;
        ImportConfiguredTeluguBible();
        _backgrounds = _data.Backgrounds;
        SyncConfiguredBackgrounds();
        RestoreBackgroundPreferences();
        LoadLogoImage();
        BuildInterface();
        _ = LoadSongsFromMongoAsync();
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
        _syncTimer.Tick += async (_, _) => await SyncFromMongoAsync();
        if (_data.Sync.UseMongoDb) _syncTimer.Start();
    }

    private async Task SyncFromMongoAsync()
    {
        if (_syncing || !_data.Sync.UseMongoDb) return;
        if (_mongoDb is null)
        {
            try { _mongoDb = new MongoDbService(_data.Sync.MongoDbConnectionString); }
            catch { return; }
        }
        _syncing = true;
        try
        {
            var remoteSongs = await _mongoDb.GetAllSongsAsync();
            var bySourceId = remoteSongs.Where(s => !string.IsNullOrEmpty(s.Id)).ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
            var changed = false;
            var pulled = 0; var merged = 0; var updated = 0;
            foreach (var remote in bySourceId.Values)
            {
                var existing = _library.FirstOrDefault(s => s.SourceId == remote.Id);
                if (existing is null)
                {
                    existing = _library.FirstOrDefault(s => s.SourceId is null && string.Equals(s.Title, remote.Title, StringComparison.OrdinalIgnoreCase));
                    if (existing is not null) { existing.SourceId = remote.Id; merged++; changed = true; }
                }
                if (existing is null) { _library.Add(new Song { SourceId = remote.Id, Title = remote.Title, Lyrics = remote.Lyrics }); pulled++; changed = true; }
                else if (string.Equals(remote.Source, "web", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(existing.Lyrics, remote.Lyrics, StringComparison.Ordinal)) { existing.Title = remote.Title; existing.Lyrics = remote.Lyrics; updated++; changed = true; }
                }
            }
            if (changed) Persist();
            FilterLibrary(_librarySearch?.Text ?? string.Empty);
            _lastSyncFailed = false;
            var (pushCount, linkCount, _) = await PushLocalSongsToMongoAsync();
            UpdateSyncStatus(true, pulled, merged, updated, pushCount, linkCount);
        }
        catch (Exception ex) { _lastSyncFailed = true; _syncError = ex.Message; UpdateSyncStatus(false); }
        finally { _syncing = false; }
    }

    private async Task<(int pushed, int linked, int pushedUpdates)> PushLocalSongsToMongoAsync()
    {
        if (_mongoDb is null) return (0, 0, 0);
        List<MongoSong> remoteSongs;
        try { remoteSongs = await _mongoDb.GetAllSongsAsync(); } catch { return (0, 0, 0); }
        var remoteByTitle = remoteSongs.Where(s => !string.IsNullOrWhiteSpace(s.Title)).ToDictionary(s => s.Title.Trim(), s => s, StringComparer.OrdinalIgnoreCase);
        var remoteById = remoteSongs.Where(s => !string.IsNullOrEmpty(s.Id)).ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
        var pushed = 0; var linked = 0; var pushedUpdates = 0; var changed = false;

        foreach (var song in _library.Where(s => s.SourceId is not null && !string.IsNullOrWhiteSpace(s.Title)).ToList())
        {
            if (!remoteById.TryGetValue(song.SourceId!, out var remote) || !string.Equals(remote.Source, "desktop", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(song.Lyrics, remote.Lyrics, StringComparison.Ordinal)) continue;
            try { var m = ToMongoSong(song); m.Tags = remote.Tags?.Count > 0 ? remote.Tags : ["church"]; m.Desktop = "true"; m.Source = "desktop"; await _mongoDb.UpdateSongAsync(song.SourceId!, m); pushedUpdates++; changed = true; } catch { }
        }

        foreach (var song in _library.Where(s => s.SourceId is null && !string.IsNullOrWhiteSpace(s.Title)).ToList())
        {
            try
            {
                if (remoteByTitle.TryGetValue(song.Title.Trim(), out var remote)) { song.SourceId = remote.Id; linked++; changed = true; continue; }
                var m = ToMongoSong(song); m.Tags = ["church"]; m.Desktop = "true"; m.Source = "desktop";
                var created = await _mongoDb.CreateSongAsync(m); song.SourceId = created.Id; pushed++; changed = true;
            } catch { }
        }
        if (changed) { Persist(); FilterLibrary(_librarySearch?.Text ?? string.Empty); }
        return (pushed, linked, pushedUpdates);
    }

    private async Task LoadSongsFromMongoAsync()
    {
        if (!_data.Sync.UseMongoDb || _mongoDb is not null) return;
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _mongoDb = new MongoDbService(_data.Sync.MongoDbConnectionString);
                var remoteSongs = await _mongoDb.GetAllSongsAsync();
                var bySourceId = remoteSongs.Where(s => !string.IsNullOrEmpty(s.Id)).ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
                foreach (var remote in bySourceId.Values)
                {
                    var existing = _library.FirstOrDefault(s => s.SourceId == remote.Id);
                    if (existing is null) { existing = _library.FirstOrDefault(s => s.SourceId is null && string.Equals(s.Title, remote.Title, StringComparison.OrdinalIgnoreCase)); if (existing is not null) existing.SourceId = remote.Id; }
                    if (existing is null) _library.Add(new Song { SourceId = remote.Id, Title = remote.Title, Lyrics = remote.Lyrics });
                    else if (string.Equals(remote.Source, "web", StringComparison.OrdinalIgnoreCase)) { existing.Title = remote.Title; existing.Lyrics = remote.Lyrics; }
                }
                Persist(); FilterLibrary(_librarySearch?.Text ?? string.Empty); _lastSyncFailed = false; UpdateSyncStatus(true);
                _ = PushLocalSongsToMongoAsync(); return;
            }
            catch { _mongoDb = null; if (attempt < maxRetries) await Task.Delay(1000 * attempt); }
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

    private void SetMongoUri()
    {
        using var dialog = new Form { Text = "MongoDB Atlas Connection", Size = new Size(560, 220), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var label = new Label { Text = "Atlas connection string:", AutoSize = true, Location = new Point(12, 12) };
        var uriBox = new TextBox { Text = _data.Sync.MongoDbConnectionString, Location = new Point(12, 38), Width = 520 };
        var hint = new Label { Text = "Restart the app after saving.", ForeColor = Color.Gray, AutoSize = true, Location = new Point(12, 68) };
        var save = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(360, 105), Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(452, 105), Width = 80 };
        dialog.Controls.AddRange([label, uriBox, hint, save, cancel]);
        dialog.AcceptButton = save; dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _data.Sync.MongoDbConnectionString = uriBox.Text.Trim(); _store.SaveSettings(_data.Sync);
        _mongoDb?.Dispose(); _mongoDb = null; _syncTimer.Stop();
        if (_data.Sync.UseMongoDb) { _ = LoadSongsFromMongoAsync(); _syncTimer.Start(); }
    }

    private async void CheckMongoConnection()
    {
        if (!_data.Sync.UseMongoDb) { MessageBox.Show(this, "No MongoDB URI configured.\nUse Set URI on the Help tab.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try { var testDb = new MongoDbService(_data.Sync.MongoDbConnectionString); var songs = await testDb.GetAllSongsAsync(); MessageBox.Show(this, $"Connected!\n\n{songs.Count} song(s) found.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show(this, $"Could not connect.\n\n{ex.Message}", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void LoadSong(Song song) => LoadSongContent(song.Title, song.Lyrics, song.Id, "Loaded from Song Library");
    private void LoadSongContent(string title, string lyrics, Guid? songId, string source)
    {
        _updating = true; _titleBox.Text = title; _lyricsBox.Text = lyrics; _currentSongId = songId; _updating = false;
        RebuildSlides(); _slideStatus.Text = source + " - " + title;
    }
    private void FilterLibrary(string text)
    {
        if (_libraryList is null) return; _libraryList.BeginUpdate(); _libraryList.Items.Clear();
        foreach (var song in _library.Where(s => s.Title.Contains(text, StringComparison.OrdinalIgnoreCase))) _libraryList.Items.Add(song);
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
        for (var index = 0; index < _slides.Count; index++) { var summary = _slides[index].Replace(Environment.NewLine, "  /  "); var verseIndex = _verseSlideIndexes.FindLastIndex(start => start <= index); var part = index == _verseSlideIndexes[verseIndex] ? "" : "b"; _slideList.Items.Add($"V{verseIndex + 1}{part}   {summary}"); }
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
    private void SetTextStyle(Color color, bool bold) { _theme.TextColor = color; _theme.Bold = bold; _fontColorButton.BackColor = color; UpdateBoldButton(); RefreshSlides(); }
    private void UpdateBoldButton() { _boldButton.BackColor = _theme.Bold ? _brand : Color.FromArgb(232, 237, 244); _boldButton.ForeColor = _theme.Bold ? Color.White : Color.FromArgb(31, 48, 68); }
    private void UpdateItalicButton() { _italicButton.BackColor = _theme.Italic ? _brand : Color.FromArgb(232, 237, 244); _italicButton.ForeColor = _theme.Italic ? Color.White : Color.FromArgb(31, 48, 68); }
    private void UpdateUnderlineButton() { _underlineButton.BackColor = _theme.Underline ? _brand : Color.FromArgb(232, 237, 244); _underlineButton.ForeColor = _theme.Underline ? Color.White : Color.FromArgb(31, 48, 68); }
    private void UpdateStrikethroughButton() { _strikethroughButton.BackColor = _theme.Strikethrough ? _brand : Color.FromArgb(232, 237, 244); _strikethroughButton.ForeColor = _theme.Strikethrough ? Color.White : Color.FromArgb(31, 48, 68); }
    private void UpdateSubSuperButtons() { _subscriptButton.BackColor = _theme.Subscript ? _brand : Color.FromArgb(232, 237, 244); _subscriptButton.ForeColor = _theme.Subscript ? Color.White : Color.FromArgb(31, 48, 68); _superscriptButton.BackColor = _theme.Superscript ? _brand : Color.FromArgb(232, 237, 244); _superscriptButton.ForeColor = _theme.Superscript ? Color.White : Color.FromArgb(31, 48, 68); }
    private void ChooseHighlightColor() { using var dialog = new ColorDialog { Color = Color.FromArgb(255, 255, 153), FullOpen = true }; if (dialog.ShowDialog(this) != DialogResult.OK) return; _highlightColorButton.BackColor = dialog.Color; RefreshSlides(); }
    private void ClearFormatting() { _theme.Bold = false; _theme.Italic = false; _theme.Underline = false; _theme.Strikethrough = false; _theme.Subscript = false; _theme.Superscript = false; _theme.TextColor = Color.Black; UpdateBoldButton(); UpdateItalicButton(); UpdateUnderlineButton(); UpdateStrikethroughButton(); UpdateSubSuperButtons(); _fontColorButton.BackColor = Color.Black; _fontColorButton.ForeColor = Color.White; RefreshSlides(); }
    private void ToggleBullets() { if (_lyricsBox.SelectionLength == 0) return; var start = _lyricsBox.SelectionStart; var text = _lyricsBox.Text; var lineStart = text.LastIndexOf('\n', start - 1) + 1; var lineEnd = text.IndexOf('\n', start); if (lineEnd < 0) lineEnd = text.Length; var line = text.Substring(lineStart, lineEnd - lineStart); if (line.TrimStart().StartsWith("• ")) _lyricsBox.Text = text.Substring(0, lineStart) + line.Replace("• ", "", StringComparison.Ordinal) + text.Substring(lineEnd); else _lyricsBox.Text = text.Substring(0, lineStart) + "• " + line.TrimStart() + text.Substring(lineEnd); _lyricsBox.SelectionStart = start; RebuildSlides(); }
    private void ToggleNumbering() { if (_lyricsBox.SelectionLength == 0) return; var start = _lyricsBox.SelectionStart; var text = _lyricsBox.Text; var lineStart = text.LastIndexOf('\n', start - 1) + 1; var lineEnd = text.IndexOf('\n', start); if (lineEnd < 0) lineEnd = text.Length; var line = text.Substring(lineStart, lineEnd - lineStart); if (System.Text.RegularExpressions.Regex.IsMatch(line.TrimStart(), @"^\d+\.\s")) _lyricsBox.Text = text.Substring(0, lineStart) + System.Text.RegularExpressions.Regex.Replace(line.TrimStart(), @"^\d+\.\s", "") + text.Substring(lineEnd); else _lyricsBox.Text = text.Substring(0, lineStart) + "1. " + line.TrimStart() + text.Substring(lineEnd); _lyricsBox.SelectionStart = start; RebuildSlides(); }
    private void AdjustIndent(int delta) { if (_lyricsBox.SelectionLength == 0) return; }
    private void UpdateProjectorStatus()
    {
        if (_statusProjector is null) return;
        var live = (_projector is { IsDisposed: false }) || _videoProjector is not null;
        _statusProjector.Text = live ? "\u25cf Projector: live" : "Projector: off";
        _statusProjector.ForeColor = live ? Color.FromArgb(150, 230, 180) : Color.White;
        if (_projectorButton is not null) { _projectorButton.Text = live ? "\u25a3  Close projector" : "\u25a3  Open projector"; _projectorButton.BackColor = live ? Color.FromArgb(35, 157, 87) : Color.FromArgb(11, 77, 132); }
    }
    private void SetStageMode(StageMode mode) { _stageMode = _stageMode == mode ? StageMode.Slide : mode; if (_stageMode == StageMode.Logo && _logoImage is null) { SetLogoPath(); if (_logoImage is null) _stageMode = StageMode.Slide; } ApplyStageToProjectors(); UpdateStageStatus(); }
    private void ApplyStageToProjectors() { _projector?.SetStage(_stageMode, _logoImage); _videoProjector?.SetStage(_stageMode, _logoImage); UpdateStageStatus(); }
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
    private static void SetStageButton(Button button, bool active) { button.BackColor = active ? Color.FromArgb(35, 157, 87) : Color.FromArgb(232, 237, 244); button.ForeColor = active ? Color.White : Color.FromArgb(31, 48, 68); }
    private void SetLogoPath() { using var dialog = new OpenFileDialog { Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*", Title = "Choose church logo" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; var path = _store.ImportLogo(dialog.FileName); if (string.IsNullOrEmpty(path)) return; _data.BackgroundPreferences.LogoPath = path; _logoImage?.Dispose(); try { _logoImage = Image.FromFile(path); } catch { _logoImage = null; } Persist(); }
    private void LoadLogoImage() { _logoImage?.Dispose(); _logoImage = null; var path = _data.BackgroundPreferences.LogoPath; if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return; try { _logoImage = Image.FromFile(path); } catch { _logoImage = null; } }
    private void ChooseTextColor() { using var dialog = new ColorDialog { Color = _theme.TextColor, FullOpen = true }; if (dialog.ShowDialog(this) != DialogResult.OK) return; _theme.TextColor = dialog.Color; _fontColorButton.BackColor = dialog.Color; RefreshSlides(); }
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
        if (disposing) { _clockTimer.Stop(); _clockTimer.Dispose(); _syncTimer.Stop(); _syncTimer.Dispose(); _theme.BackgroundImage?.Dispose(); _videoProjector?.Close(); _mongoDb?.Dispose(); }
        base.Dispose(disposing);
    }
}
