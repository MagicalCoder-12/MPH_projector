namespace ChurchProjector;

public partial class Form1
{
    private Control BuildBibleRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        var library = RibbonGroup("Bible library", 355);
        var newBible = Button("+ New Bible", _brand, Color.White, 94, 34);
        newBible.Click += (_, _) => NewBible();
        var import = Button("Import JSON", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 102, 34);
        import.Click += (_, _) => ImportBible();
        var delete = Button("Delete Bible", Color.White, Color.FromArgb(177, 59, 54), 98, 34);
        delete.Click += (_, _) => DeleteCurrentBible();
        Add(library, newBible, import, delete, Hint("Import a Bible translation stored as JSON."));

        var present = RibbonGroup("Present", 280);
        var show = Button("Show selected verse", Color.FromArgb(35, 157, 87), Color.White, 145, 34);
        show.Click += (_, _) => ShowSelectedBibleVerse();
        var projector = Button("Open projector", Color.White, Color.FromArgb(31, 48, 68), 110, 34);
        projector.Click += (_, _) => ToggleProjector();
        Add(present, show, projector, Hint("The selected verse is shown in the preview."));
        ribbon.Controls.AddRange([library, present]);
        return ribbon;
    }

    private Control BuildBibleWorkspace()
    {
        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(10, 10, 10, 12), BackColor = Color.FromArgb(241, 244, 247) };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.Controls.Add(BuildBibleLibraryPanel(), 0, 0);
        workspace.Controls.Add(BuildBibleEditorPanel(), 1, 0);
        workspace.Controls.Add(BuildBiblePreviewPanel(), 2, 0);
        return workspace;
    }

    private Control BuildBibleLibraryPanel()
    {
        var outer = Section("Bible library", "Your Bible translations");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 2;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, WrapContents = false, Margin = new Padding(0, 0, 0, 8) };
        var add = Button("+ New", _brand, Color.White, 65, 32);
        add.Click += (_, _) => NewBible();
        var import = Button("Import", Color.White, Color.FromArgb(31, 48, 68), 70, 32);
        import.Click += (_, _) => ImportBible();
        var delete = Button("Delete", Color.White, Color.FromArgb(177, 59, 54), 65, 32);
        delete.Click += (_, _) => DeleteCurrentBible();
        actions.Controls.AddRange([add, import, delete]);
        _bibleList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F) };
        _bibleList.SelectedIndexChanged += (_, _) => { if (!_updating && _bibleList.SelectedItem is BibleTranslation bible) LoadBible(bible); };
        content.Controls.Add(actions, 0, 0);
        content.Controls.Add(_bibleList, 0, 1);
        RefreshBibleList();
        return outer;
    }

    private Control BuildBibleEditorPanel()
    {
        var outer = Section("Bible editor", "Add, edit, and present individual verses");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 7;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 48));

        content.Controls.Add(SmallLabel("BIBLE NAME"), 0, 0);
        _bibleNameBox = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 11F, FontStyle.Bold), PlaceholderText = "e.g. My Church Bible", Margin = new Padding(0, 0, 0, 8) };
        content.Controls.Add(_bibleNameBox, 0, 1);
        var reference = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 55, WrapContents = false, Margin = new Padding(0, 0, 0, 7) };
        _bibleBookBox = new TextBox { Width = 160, PlaceholderText = "Book", Margin = new Padding(0, 19, 8, 0) };
        _bibleChapter = new NumericUpDown { Minimum = 1, Maximum = 200, Value = 1, Width = 62 };
        _bibleVerseNumber = new NumericUpDown { Minimum = 1, Maximum = 200, Value = 1, Width = 62 };
        reference.Controls.Add(_bibleBookBox);
        reference.Controls.Add(Field("Chapter", _bibleChapter));
        reference.Controls.Add(Field("Verse", _bibleVerseNumber));
        content.Controls.Add(reference, 0, 2);
        _bibleVerseText = new RichTextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12F), Margin = new Padding(0, 0, 0, 8) };
        content.Controls.Add(_bibleVerseText, 0, 3);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, WrapContents = false, Margin = new Padding(0, 0, 0, 7) };
        var saveBible = Button("Save Bible", Color.White, Color.FromArgb(31, 48, 68), 88, 32);
        saveBible.Click += (_, _) => SaveBible();
        var save = Button("Save verse", _brand, Color.White, 90, 32);
        save.Click += (_, _) => SaveBibleVerse();
        var remove = Button("Delete verse", Color.White, Color.FromArgb(177, 59, 54), 95, 32);
        remove.Click += (_, _) => DeleteCurrentBibleVerse();
        var show = Button("Show verse", Color.FromArgb(35, 157, 87), Color.White, 90, 32);
        show.Click += (_, _) => ShowSelectedBibleVerse();
        actions.Controls.AddRange([saveBible, save, remove, show]);
        content.Controls.Add(actions, 0, 4);
        content.Controls.Add(SmallLabel("VERSES"), 0, 5);
        _bibleVerseList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 9.5F) };
        _bibleVerseList.SelectedIndexChanged += (_, _) => { if (!_updating && _bibleVerseList.SelectedItem is BibleVerse verse) LoadBibleVerse(verse); };
        content.Controls.Add(_bibleVerseList, 0, 6);
        return outer;
    }

    private Control BuildBiblePreviewPanel()
    {
        var outer = Section("Bible preview", "The selected verse ready for projection");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 3;
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _biblePreview = new SlideCanvas { Dock = DockStyle.Fill, Theme = _theme, Margin = new Padding(0, 0, 0, 9), BackColor = Color.FromArgb(22, 28, 37) };
        var show = Button("Show selected verse", Color.FromArgb(35, 157, 87), Color.White, 145, 34);
        show.Click += (_, _) => ShowSelectedBibleVerse();
        var tip = new Label { Text = "Select a verse from the list to preview it.", ForeColor = Color.FromArgb(112, 125, 138), AutoSize = true, Margin = new Padding(0, 7, 0, 0) };
        content.Controls.Add(_biblePreview, 0, 0);
        content.Controls.Add(show, 0, 1);
        content.Controls.Add(tip, 0, 2);
        return outer;
    }

    private void NewBible()
    {
        _currentBibleId = null;
        _currentBibleVerseId = null;
        if (_bibleNameBox is null) return;
        _updating = true;
        _bibleNameBox.Text = "";
        _bibleBookBox.Text = "Genesis";
        _bibleChapter.Value = 1;
        _bibleVerseNumber.Value = 1;
        _bibleVerseText.Text = "";
        _bibleVerseList.Items.Clear();
        if (_bibleList is not null) _bibleList.ClearSelected();
        _updating = false;
        _bibleNameBox.Focus();
    }

    private void RefreshBibleList()
    {
        if (_bibleList is null) return;
        _updating = true;
        _bibleList.BeginUpdate();
        _bibleList.Items.Clear();
        foreach (var bible in _bibles.OrderBy(item => item.Name)) _bibleList.Items.Add(bible);
        _bibleList.EndUpdate();
        _updating = false;
    }

    private void LoadBible(BibleTranslation bible)
    {
        _currentBibleId = bible.Id;
        _currentBibleVerseId = null;
        _updating = true;
        _bibleNameBox.Text = bible.Name;
        _bibleBookBox.Text = "Genesis";
        _bibleChapter.Value = 1;
        _bibleVerseNumber.Value = 1;
        _bibleVerseText.Text = "";
        _updating = false;
        RefreshBibleVerses();
    }

    private void RefreshBibleVerses()
    {
        if (_bibleVerseList is null) return;
        var bible = _currentBibleId is Guid id ? _bibles.FirstOrDefault(item => item.Id == id) : null;
        _updating = true;
        _bibleVerseList.BeginUpdate();
        _bibleVerseList.Items.Clear();
        if (bible is not null)
            foreach (var verse in bible.Verses.OrderBy(item => item.Book).ThenBy(item => item.Chapter).ThenBy(item => item.Verse)) _bibleVerseList.Items.Add(verse);
        _bibleVerseList.EndUpdate();
        _updating = false;
    }

    private void SaveBibleVerse()
    {
        var name = _bibleNameBox.Text.Trim();
        var book = _bibleBookBox.Text.Trim();
        var text = _bibleVerseText.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(book) || string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "Enter a Bible name, a book, and verse text before saving.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var bible = _currentBibleId is Guid id ? _bibles.FirstOrDefault(item => item.Id == id) : null;
        if (bible is null)
        {
            bible = new BibleTranslation();
            _bibles.Add(bible);
            _currentBibleId = bible.Id;
        }
        bible.Name = name;
        var verse = _currentBibleVerseId is Guid verseId ? bible.Verses.FirstOrDefault(item => item.Id == verseId) : null;
        if (verse is null)
        {
            verse = new BibleVerse();
            bible.Verses.Add(verse);
            _currentBibleVerseId = verse.Id;
        }
        verse.Book = book;
        verse.Chapter = (int)_bibleChapter.Value;
        verse.Verse = (int)_bibleVerseNumber.Value;
        verse.Text = text;
        Persist();
        RefreshBibleList();
        RefreshBibleVerses();
        _bibleVerseList.SelectedItem = verse;
        ShowBibleVerse(bible, verse);
    }

    private void SaveBible()
    {
        var name = _bibleNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Enter a Bible name before saving.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _bibleNameBox.Focus();
            return;
        }
        var bible = _currentBibleId is Guid id ? _bibles.FirstOrDefault(item => item.Id == id) : null;
        if (bible is null)
        {
            bible = new BibleTranslation();
            _bibles.Add(bible);
            _currentBibleId = bible.Id;
        }
        bible.Name = name;
        Persist();
        RefreshBibleList();
        _bibleList.SelectedItem = bible;
    }

    private void LoadBibleVerse(BibleVerse verse)
    {
        var bible = _currentBibleId is Guid id ? _bibles.FirstOrDefault(item => item.Id == id) : null;
        if (bible is null) return;
        _currentBibleVerseId = verse.Id;
        _updating = true;
        _bibleBookBox.Text = verse.Book;
        _bibleChapter.Value = verse.Chapter;
        _bibleVerseNumber.Value = verse.Verse;
        _bibleVerseText.Text = verse.Text;
        _updating = false;
        ShowBibleVerse(bible, verse);
    }

    private void DeleteCurrentBibleVerse()
    {
        var bible = _currentBibleId is Guid id ? _bibles.FirstOrDefault(item => item.Id == id) : null;
        var verse = _currentBibleVerseId is Guid verseId ? bible?.Verses.FirstOrDefault(item => item.Id == verseId) : null;
        if (bible is null || verse is null) return;
        bible.Verses.Remove(verse);
        _currentBibleVerseId = null;
        _bibleVerseText.Text = "";
        Persist();
        RefreshBibleVerses();
    }

    private void DeleteCurrentBible()
    {
        var bible = _currentBibleId is Guid id ? _bibles.FirstOrDefault(item => item.Id == id) : null;
        if (bible is null) return;
        if (MessageBox.Show(this, $"Delete '{bible.Name}' and all of its verses?", "MPH Songs", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _bibles.Remove(bible);
        Persist();
        RefreshBibleList();
        NewBible();
    }

    private void ImportBible()
    {
        using var dialog = new OpenFileDialog { Filter = "Bible files|*.json;*.db;*.vpc|SQLite Bible database|*.db|JSON Bible|*.json|VideoPsalm package|*.vpc|All files|*.*", Title = "Import Bible translation" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var bible = string.Equals(Path.GetExtension(dialog.FileName), ".db", StringComparison.OrdinalIgnoreCase)
                ? _store.ImportSqliteBible(dialog.FileName)
                : _store.ImportBible(dialog.FileName);
            _bibles.Add(bible);
            Persist();
            RefreshBibleList();
            _bibleList.SelectedItem = bible;
            LoadBible(bible);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, "This Bible file could not be imported.\n\n" + exception.Message, "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowSelectedBibleVerse()
    {
        var bible = _currentBibleId is Guid id ? _bibles.FirstOrDefault(item => item.Id == id) : null;
        var verse = _bibleVerseList?.SelectedItem as BibleVerse;
        if (bible is not null && verse is not null) ShowBibleVerse(bible, verse);
    }

    private void ShowBibleVerse(BibleTranslation bible, BibleVerse verse)
    {
        _slides.Clear();
        _slides.Add($"{verse.Text}{Environment.NewLine}{Environment.NewLine}{verse.Reference} ({bible.Name})");
        _verseSlideIndexes.Clear();
        _verseSlideIndexes.Add(0);
        _currentSlide = 0;
        RefreshSlides();
    }
}
