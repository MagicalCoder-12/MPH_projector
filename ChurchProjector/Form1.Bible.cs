namespace ChurchProjector;

public partial class Form1
{
    private Control BuildBibleRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        EnableHorizontalWheel(ribbon);
        var library = RibbonGroup("Bible library", 380);
        var newBible = Button("+ New Bible", _brand, Color.White, 100, 34);
        newBible.Click += (_, _) => NewBible();
        Tip(newBible, "Create a new Bible translation to fill with verses");
        var import = Button("Import file", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 96, 34);
        import.Click += (_, _) => ImportBible();
        Tip(import, "Import a Bible translation from a JSON or SQLite file");
        var delete = Button("Delete Bible", Color.White, Color.FromArgb(177, 59, 54), 100, 34);
        delete.Click += (_, _) => DeleteCurrentBible();
        Tip(delete, "Delete the current translation and all of its verses");
        Add(library, newBible, import, delete, Hint("Import a Bible translation stored as JSON, or the Telugu SQLite database."));

        var present = RibbonGroup("Present", 300);
        var show = Button("Preview verse", Color.FromArgb(35, 157, 87), Color.White, 112, 34);
        show.Click += (_, _) => ShowSelectedBibleVerse();
        Tip(show, "Show the selected verse in the preview pane");
        var projector = Button("Open projector", Color.White, Color.FromArgb(31, 48, 68), 116, 34);
        projector.Click += (_, _) => ToggleProjector();
        Tip(projector, "Open or close the borderless projector window (F5)");
        Add(present, show, projector, Hint("The selected verse is shown in the preview."));
        ribbon.Controls.AddRange([library, present]);
        return ribbon;
    }

    private Control BuildBibleWorkspace()
    {
        var outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 10, 10, 12), BackColor = Color.FromArgb(241, 244, 247) };
        var navSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, BackColor = Color.FromArgb(241, 244, 247), SplitterWidth = 6 };
        navSplit.Panel1.Controls.Add(BuildBibleNavigatorPanel());
        var right = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, BackColor = Color.FromArgb(241, 244, 247), SplitterWidth = 6 };
        right.Panel1.Controls.Add(BuildBibleVersesPanel());
        var rightVertical = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, BackColor = Color.FromArgb(241, 244, 247), SplitterWidth = 6 };
        rightVertical.Panel1.Controls.Add(BuildBiblePreviewPanel());
        rightVertical.Panel2.Controls.Add(BuildBibleAgendaPanel());
        right.Panel2.Controls.Add(rightVertical);
        navSplit.Panel2.Controls.Add(right);
        LayoutSplit(navSplit, outer, 0.28f);
        LayoutSplit(right, right, 44f / 72f);
        LayoutSplit(rightVertical, rightVertical, 0.62f);
        outer.Controls.Add(navSplit);
        RefreshBibleVerses();
        return outer;
    }

    private Control BuildBibleAgendaPanel()
    {
        var outer = Section("Service agenda", "Order Bible passages for this service");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 3;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false, Margin = new Padding(0) };
        var remove = Button("Remove", Color.White, Color.FromArgb(177, 59, 54), 70, 32);
        remove.Click += (_, _) => RemoveAgendaItem();
        Tip(remove, "Remove the selected Bible passage from the service order");
        var up = Button("▲", Color.White, Color.FromArgb(31, 48, 68), 40, 32);
        up.Click += (_, _) => MoveAgendaItem(-1);
        Tip(up, "Move the selected passage earlier");
        var down = Button("▼", Color.White, Color.FromArgb(31, 48, 68), 52, 32);
        down.Click += (_, _) => MoveAgendaItem(1);
        Tip(down, "Move the selected passage later");
        actions.Controls.AddRange([remove, up, down]);
        _bibleAgendaList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F) };
        _bibleAgendaList.DoubleClick += (_, _) => { if (_bibleAgendaList.SelectedItem is AgendaItem) LoadAgendaItem(); };
        _bibleAgendaList.SelectedIndexChanged += (_, _) => { if (!_updating && _bibleAgendaList.SelectedItem is AgendaItem) LoadAgendaItem(); };
        content.Controls.Add(actions, 0, 0);
        content.Controls.Add(_bibleAgendaList, 0, 1);
        RefreshBibleAgenda();
        return outer;
    }

    private Control BuildBibleNavigatorPanel()
    {
        var outer = Section("Bible", "Choose a translation, book, and chapter");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 4;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(SmallLabel("TRANSLATION"), 0, 0);
        _bibleTranslationPicker = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, 8), DropDownWidth = 220 };
        _bibleTranslationPicker.SelectedIndexChanged += (_, _) => { if (!_updating && _bibleTranslationPicker.SelectedItem is BibleTranslation bible) LoadBible(bible); };
        content.Controls.Add(_bibleTranslationPicker, 0, 1);

        var reference = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false, Margin = new Padding(0, 0, 0, 8) };
        _bibleReferenceBox = new TextBox { Width = 130, PlaceholderText = "e.g. John 3:16", Margin = new Padding(0, 4, 6, 0) };
        _bibleReferenceBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) GoToReference(); };
        Tip(_bibleReferenceBox, "Type a Bible reference and press Enter");
        var go = Button("Go", _brand, Color.White, 48, 30);
        go.Click += (_, _) => GoToReference();
        Tip(go, "Jump to a Bible reference such as John 3:16 or Psalm 23:1-6");
        reference.Controls.AddRange([_bibleReferenceBox, go]);
        content.Controls.Add(reference, 0, 2);

        var navigation = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        navigation.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        navigation.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        navigation.Controls.Add(SmallLabel("BOOKS"), 0, 0);
        navigation.Controls.Add(SmallLabel("CHAPTER"), 1, 0);
        _bibleBookList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F) };
        _bibleBookList.SelectedIndexChanged += (_, _) => { if (!_updating) RefreshBibleChapters(); };
        _bibleChapterList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F) };
        _bibleChapterList.SelectedIndexChanged += (_, _) => { if (!_updating) RefreshBibleVerses(); };
        navigation.Controls.Add(_bibleBookList, 0, 1);
        navigation.Controls.Add(_bibleChapterList, 1, 1);
        content.Controls.Add(navigation, 0, 3);
        RefreshBibleTranslationPicker();
        return outer;
    }

    private Control BuildBibleVersesPanel()
    {
        var outer = Section("Verses", "Preview, edit, or add selected verses to the service");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 3;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _bibleReferenceLabel = new Label { Text = "Choose a Bible book and chapter", AutoSize = true, ForeColor = Color.FromArgb(52, 94, 130), Font = new Font("Segoe UI", 10F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 7) };
        _bibleVerseList = new VerseListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, 0, 7) };
        _bibleVerseList.SelectedIndexChanged += (_, _) => { if (!_updating && _bibleVerseList.SelectedItems.Count > 0) PreviewSelectedBibleVerses(); };

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 74, WrapContents = true, Margin = new Padding(0), Padding = new Padding(0) };
        var addVerse = Button("+ New verse", _brand, Color.White, 100, 34);
        addVerse.Click += (_, _) => CreateBibleVerse();
        Tip(addVerse, "Create a verse in the current translation");
        var editVerse = Button("Edit", Color.White, Color.FromArgb(31, 48, 68), 72, 34);
        editVerse.Click += (_, _) => EditBibleVerse();
        Tip(editVerse, "Edit the selected verse");
        var removeVerse = Button("Delete", Color.White, Color.FromArgb(177, 59, 54), 78, 34);
        removeVerse.Click += (_, _) => DeleteBibleVerse();
        Tip(removeVerse, "Delete the selected verse(s)");
        var add = Button("+ Add to agenda", Color.White, Color.FromArgb(31, 48, 68), 112, 34);
        add.Click += (_, _) => AddSelectedBibleVerseToAgenda();
        Tip(add, "Add the selected passage to the service order");
        var show = Button("● Show live", Color.FromArgb(35, 157, 87), Color.White, 100, 34);
        show.Click += (_, _) => ShowLiveVerse();
        Tip(show, "Project the selected verses on the live display (F5)");
        actions.Controls.AddRange([addVerse, editVerse, removeVerse, add, show]);

        content.Controls.Add(_bibleReferenceLabel, 0, 0);
        content.Controls.Add(_bibleVerseList, 0, 1);
        content.Controls.Add(actions, 0, 2);
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
        var show = Button("Show selected verse", Color.FromArgb(35, 157, 87), Color.White, 150, 34);
        show.Click += (_, _) => ShowSelectedBibleVerse();
        Tip(show, "Preview the selected verse in this pane");
        var tip = new Label { Text = "Select a verse from the list to preview it.", ForeColor = Color.FromArgb(112, 125, 138), AutoSize = true, Margin = new Padding(0, 7, 0, 0) };
        content.Controls.Add(_biblePreview, 0, 0);
        content.Controls.Add(show, 0, 1);
        content.Controls.Add(tip, 0, 2);
        return outer;
    }

    private BibleTranslation? CurrentBible() => _currentBibleId is Guid id ? _bibles.FirstOrDefault(item => item.Id == id) : null;

    private void NewBible()
    {
        var name = DialogHelpers.PromptText(this, "New Bible translation", "Translation name:");
        if (string.IsNullOrWhiteSpace(name)) return;
        var bible = new BibleTranslation { Name = name };
        _bibles.Add(bible);
        _currentBibleId = bible.Id;
        _currentBibleVerseId = null;
        Persist();
        RefreshBibleTranslationPicker();
        LoadBible(bible);
        if (_bibleReferenceLabel is not null)
            _bibleReferenceLabel.Text = "New translation — use + New verse to add the first verse";
        _slideStatus.Text = "Created Bible translation: " + bible.Name;
    }

    private void LoadBible(BibleTranslation bible)
    {
        _currentBibleId = bible.Id;
        _currentBibleVerseId = null;
        _updating = true;
        if (_bibleTranslationPicker is not null && _bibleTranslationPicker.SelectedItem != bible)
            _bibleTranslationPicker.SelectedItem = bible;
        _updating = false;
        RefreshBibleBooks();
        if (_bibleReferenceLabel is not null)
            _bibleReferenceLabel.Text = bible.Verses.Count == 0
                ? "This translation has no verses yet — use + New verse"
                : "Select a book and chapter";
    }

    private void RefreshBibleVerses()
    {
        if (_bibleVerseList is null) return;
        var bible = CurrentBible();
        var book = _bibleBookList?.SelectedItem as string;
        var chapter = _bibleChapterList?.SelectedItem is int selectedChapter ? selectedChapter : (int?)null;
        _updating = true;
        _bibleVerseList.BeginUpdate();
        _bibleVerseList.Items.Clear();
        if (bible is not null)
        {
            var verses = bible.Verses.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(book)) verses = verses.Where(item => item.Book == book);
            if (chapter is not null) verses = verses.Where(item => item.Chapter == chapter.Value);
            foreach (var verse in verses.OrderBy(item => item.Verse)) _bibleVerseList.Items.Add(verse);
        }
        _bibleVerseList.EndUpdate();
        _updating = false;
        if (_bibleReferenceLabel is not null)
            _bibleReferenceLabel.Text = string.IsNullOrWhiteSpace(book) ? "Choose a Bible book and chapter" : chapter is null ? book : $"{book} {chapter}";
    }

    private void RefreshBibleTranslationPicker()
    {
        if (_bibleTranslationPicker is null) return;
        var current = _currentBibleId;
        _updating = true;
        _bibleTranslationPicker.BeginUpdate();
        _bibleTranslationPicker.Items.Clear();
        foreach (var bible in _bibles.OrderBy(item => item.Name)) _bibleTranslationPicker.Items.Add(bible);
        _bibleTranslationPicker.SelectedItem = current is Guid id ? _bibles.FirstOrDefault(item => item.Id == id) : null;
        _bibleTranslationPicker.EndUpdate();
        _updating = false;
        if (_bibleTranslationPicker.SelectedItem is not BibleTranslation && _bibles.Count > 0) LoadBible(_bibles.OrderBy(item => item.Name).First());
    }

    private void RefreshBibleBooks()
    {
        if (_bibleBookList is null) return;
        var bible = CurrentBible();
        _updating = true;
        _bibleBookList.BeginUpdate();
        _bibleBookList.Items.Clear();
        if (bible is not null)
        {
            var available = bible.Verses.Select(item => item.Book).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var book in BibleBooks.Canonical.Where(available.Contains)) _bibleBookList.Items.Add(book);
            foreach (var book in available.Where(book => !BibleBooks.Canonical.Contains(book, StringComparer.OrdinalIgnoreCase)).OrderBy(book => book)) _bibleBookList.Items.Add(book);
            if (_bibleBookList.Items.Count > 0) _bibleBookList.SelectedIndex = 0;
        }
        _bibleBookList.EndUpdate();
        _updating = false;
        RefreshBibleChapters();
    }

    private void GoToReference()
    {
        if (_bibleReferenceBox is null) return;
        var input = _bibleReferenceBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;
        var bible = CurrentBible();
        if (bible is null)
        {
            MessageBox.Show(this, "Choose or import a Bible translation first.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var match = System.Text.RegularExpressions.Regex.Match(input, @"^(?<book>.+?)\s+(\d+)\s*:\s*(\d+)(?:\s*-\s*(\d+))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            MessageBox.Show(this, "Enter a reference like  John 3:16  or  Psalm 23:1-6", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var book = FindBook(bible, match.Groups["book"].Value.Trim());
        if (book is null)
        {
            MessageBox.Show(this, $"No book named '{match.Groups["book"].Value.Trim()}' was found in this translation.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var chapter = int.Parse(match.Groups[2].Value);
        var startVerse = int.Parse(match.Groups[3].Value);
        var endVerse = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : startVerse;

        _updating = true;
        _bibleBookList.SelectedItem = book;
        RefreshBibleChapters();
        foreach (int item in _bibleChapterList.Items)
            if (item == chapter) { _bibleChapterList.SelectedItem = item; break; }
        RefreshBibleVerses();
        _bibleVerseList.BeginUpdate();
        _bibleVerseList.ClearSelected();
        for (var index = 0; index < _bibleVerseList.Items.Count; index++)
        {
            if (_bibleVerseList.Items[index] is BibleVerse verse && verse.Verse >= startVerse && verse.Verse <= endVerse)
                _bibleVerseList.SetSelected(index, true);
        }
        _bibleVerseList.EndUpdate();
        _updating = false;
        PreviewSelectedBibleVerses();
    }

    private static string? FindBook(BibleTranslation bible, string input)
    {
        var normalised = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        return bible.Verses.Select(item => item.Book).Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(book => new string(book.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant() == normalised);
    }

    private void RefreshBibleChapters()
    {
        if (_bibleChapterList is null) return;
        var bible = CurrentBible();
        var book = _bibleBookList?.SelectedItem as string;
        _updating = true;
        _bibleChapterList.BeginUpdate();
        _bibleChapterList.Items.Clear();
        if (bible is not null && !string.IsNullOrWhiteSpace(book))
        {
            foreach (var chapter in bible.Verses.Where(item => item.Book == book).Select(item => item.Chapter).Distinct().OrderBy(item => item)) _bibleChapterList.Items.Add(chapter);
            if (_bibleChapterList.Items.Count > 0) _bibleChapterList.SelectedIndex = 0;
        }
        _bibleChapterList.EndUpdate();
        _updating = false;
        RefreshBibleVerses();
    }

    private void RefreshBibleAgenda()
    {
        if (_bibleAgendaList is null) return;
        _updating = true;
        _bibleAgendaList.BeginUpdate();
        _bibleAgendaList.Items.Clear();
        foreach (var item in _agenda) _bibleAgendaList.Items.Add(item);
        _bibleAgendaList.EndUpdate();
        _updating = false;
    }

    private void AddSelectedBibleVerseToAgenda()
    {
        var bible = CurrentBible();
        if (bible is null) return;
        var verses = _bibleVerseList.SelectedItems.Cast<BibleVerse>().OrderBy(item => item.Book).ThenBy(item => item.Chapter).ThenBy(item => item.Verse).ToList();
        if (verses.Count == 0) return;
        var first = verses[0];
        var last = verses[^1];
        var title = verses.Count == 1
            ? $"{first.Reference} ({bible.Name})"
            : first.Book == last.Book && first.Chapter == last.Chapter
                ? $"{first.Book} {first.Chapter}:{first.Verse}-{last.Verse} ({bible.Name})"
                : $"{first.Reference} - {last.Reference} ({bible.Name})";
        _agenda.Add(new AgendaItem
        {
            Title = title,
            LyricsSnapshot = string.Join(Environment.NewLine + Environment.NewLine, verses.Select(verse => $"{verse.Text}{Environment.NewLine}{verse.Reference}"))
        });
        Persist();
        RefreshAgenda();
        RefreshBibleAgenda();
        if (_bibleAgendaList is not null) _bibleAgendaList.SelectedIndex = _agenda.Count - 1;
        _slideStatus.Text = "Added to service agenda: " + title;
    }

    private void CreateBibleVerse()
    {
        var bible = CurrentBible();
        if (bible is null)
        {
            MessageBox.Show(this, "Create or import a Bible translation first, then add verses to it.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new BibleVerseEditorForm(bible.Name);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var verse = new BibleVerse
        {
            Book = dialog.Book,
            Chapter = dialog.Chapter,
            Verse = dialog.VerseNumber,
            Text = dialog.VerseText
        };
        bible.Verses.Add(verse);
        Persist();
        RefreshBibleVerses();
        SelectVerseInList(verse);
        ShowBibleVerses(bible, [verse]);
        _slideStatus.Text = "Saved verse " + verse.Reference;
    }

    private void EditBibleVerse()
    {
        var bible = CurrentBible();
        if (bible is null || _bibleVerseList is null) return;
        if (_bibleVerseList.SelectedItems.Count != 1)
        {
            MessageBox.Show(this, "Select exactly one verse to edit.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var verse = (BibleVerse)_bibleVerseList.SelectedItems[0]!;
        using var dialog = new BibleVerseEditorForm(bible.Name, verse);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        verse.Book = dialog.Book;
        verse.Chapter = dialog.Chapter;
        verse.Verse = dialog.VerseNumber;
        verse.Text = dialog.VerseText;
        Persist();
        RefreshBibleVerses();
        SelectVerseInList(verse);
        ShowBibleVerse(bible, verse);
        _slideStatus.Text = "Updated verse " + verse.Reference;
    }

    private void DeleteBibleVerse()
    {
        var bible = CurrentBible();
        if (bible is null || _bibleVerseList is null) return;
        if (_bibleVerseList.SelectedItems.Count == 0)
        {
            MessageBox.Show(this, "Select a verse to delete.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var verses = _bibleVerseList.SelectedItems.Cast<BibleVerse>().ToList();
        var message = verses.Count == 1
            ? $"Delete verse {verses[0].Reference}?\n\nThis cannot be undone."
            : $"Delete the {verses.Count} selected verses?\n\nThis cannot be undone.";
        if (!DialogHelpers.Confirm(this, "Delete verse", message, destructive: true)) return;
        foreach (var verse in verses) bible.Verses.Remove(verse);
        _currentBibleVerseId = null;
        Persist();
        RefreshBibleVerses();
        _slideStatus.Text = verses.Count == 1 ? "Deleted verse" : $"Deleted {verses.Count} verses";
    }

    private void SelectVerseInList(BibleVerse verse)
    {
        if (_bibleVerseList is null) return;
        var index = _bibleVerseList.Items.IndexOf(verse);
        if (index >= 0)
        {
            _updating = true;
            _bibleVerseList.SelectedIndex = index;
            _updating = false;
        }
    }

    private void DeleteCurrentBible()
    {
        var bible = CurrentBible();
        if (bible is null)
        {
            MessageBox.Show(this, "Choose a Bible translation to delete first.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!DialogHelpers.Confirm(this, "Delete Bible translation",
                $"Delete '{bible.Name}' and all of its verses?\n\nThis cannot be undone.", destructive: true)) return;
        _bibles.Remove(bible);
        _currentBibleId = null;
        _currentBibleVerseId = null;
        Persist();
        RefreshBibleTranslationPicker();
        if (_bibleTranslationPicker.SelectedItem is not BibleTranslation)
        {
            RefreshBibleBooks();
            if (_bibleReferenceLabel is not null) _bibleReferenceLabel.Text = "Choose or create a Bible translation";
        }
        _slideStatus.Text = "Deleted Bible translation: " + bible.Name;
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
            _currentBibleId = bible.Id;
            _currentBibleVerseId = null;
            Persist();
            RefreshBibleTranslationPicker();
            LoadBible(bible);
            _slideStatus.Text = "Imported Bible translation: " + bible.Name;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, "This Bible file could not be imported.\n\n" + exception.Message, "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowSelectedBibleVerse()
    {
        var bible = CurrentBible();
        if (bible is not null) PreviewSelectedBibleVerses();
    }

    private void ShowLiveVerse()
    {
        ShowSelectedBibleVerse();
        ToggleProjector();
    }

    private void PreviewSelectedBibleVerses()
    {
        var bible = CurrentBible();
        if (bible is null || _bibleVerseList is null) return;
        var verses = _bibleVerseList.SelectedItems.Cast<BibleVerse>().OrderBy(item => item.Book).ThenBy(item => item.Chapter).ThenBy(item => item.Verse).ToList();
        if (verses.Count == 0) return;
        _currentBibleVerseId = verses[0].Id;
        ShowBibleVerses(bible, verses);
    }

    private void ShowBibleVerse(BibleTranslation bible, BibleVerse verse)
        => ShowBibleVerses(bible, [verse]);

    private void ShowBibleVerses(BibleTranslation bible, IEnumerable<BibleVerse> verses)
    {
        _slides.Clear();
        foreach (var verse in verses)
            _slides.Add($"{verse.Text}{Environment.NewLine}{Environment.NewLine}{verse.Reference} ({bible.Name})");
        _verseSlideIndexes.Clear();
        for (var index = 0; index < _slides.Count; index++) _verseSlideIndexes.Add(index);
        _currentSlide = 0;
        RefreshSlides();
    }
}
