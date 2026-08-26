namespace ChurchProjector;

public partial class Form1
{
    private Control BuildHelpRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        EnableHorizontalWheel(ribbon);
        var start = RibbonGroup("Getting started", 330);
        Add(start, Hint("1. Open or create a song.  2. Add it to the service agenda.  3. Choose a slide.  4. Press F5 to project."));
        var keyboard = RibbonGroup("Keyboard shortcuts", 330);
        Add(keyboard, Hint("Arrows or Page Up/Page Down: slides.  1-9: jump to song verses.  Ctrl+S: save.  F5: projector.  Esc: close projector."));
        var storage = RibbonGroup("Library", 275);
        Add(storage, Hint("Songs, agenda items, Bible translations, and verses are saved automatically on this computer."));
        var backup = RibbonGroup("Backup", 250);
        var export = Button("Export", _brand, Color.White, 90, 34);
        export.Click += (_, _) => ExportLibrary();
        Tip(export, "Save the whole library to a .mphbundle file");
        var import = Button("Import", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 90, 34);
        import.Click += (_, _) => ImportLibrary();
        Tip(import, "Restore a library from a .mphbundle file");
        Add(backup, export, import, Hint("Export a .mphbundle to move your whole library to another computer."));
        var sync = RibbonGroup("Cloud sync", 430);
        var syncConfig = Button("Set URI", _brand, Color.White, 80, 34);
        syncConfig.Click += (_, _) => SetMongoUri();
        Tip(syncConfig, "Enter your MongoDB Atlas connection string");
        var syncStatus = Button("Check connection", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 120, 34);
        syncStatus.Click += (_, _) => CheckMongoConnection();
        Tip(syncStatus, "Test the connection to MongoDB Atlas");
        var syncRefresh = Button("Refresh now", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 95, 34);
        syncRefresh.Click += async (_, _) => await SyncFromMongoAsync();
        Tip(syncRefresh, "Sync with the cloud straight away");
        var syncDisable = Button("Disable", Color.White, Color.FromArgb(177, 59, 54), 80, 34);
        syncDisable.Click += (_, _) => DisableMongoSync();
        Tip(syncDisable, "Stop cloud sync and work only from the local library");
        Add(sync, syncConfig, syncStatus, syncRefresh, syncDisable, Hint("Connect to MongoDB Atlas to share songs with mph-songs.vercel.app."));
        var about = RibbonGroup("About", 190);
        var aboutButton = Button("About MPH Songs", Color.FromArgb(232, 237, 244), Color.FromArgb(31, 48, 68), 140, 34);
        aboutButton.Click += (_, _) => ShowAbout();
        Tip(aboutButton, "Version and library information");
        Add(about, aboutButton);
        ribbon.Controls.AddRange([start, keyboard, storage, backup, sync, about]);
        return ribbon;
    }

    private void ShowAbout()
    {
        var version = typeof(Form1).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        var libraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MPH Songs");
        using var dialog = new Form
        {
            Text = "About MPH Songs",
            ClientSize = new Size(460, 300),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.White
        };
        var banner = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = _brand };
        banner.Controls.Add(new Label
        {
            Text = "✦  MPH SONGS",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(18, 12)
        });
        banner.Controls.Add(new Label
        {
            Text = "Song and Bible projection for church services",
            ForeColor = Color.FromArgb(213, 235, 251),
            AutoSize = true,
            Location = new Point(20, 45)
        });
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(18, 12, 18, 0) };
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(new Label { Text = $"Version {version}", AutoSize = true, ForeColor = Color.FromArgb(31, 48, 68), Font = new Font("Segoe UI", 10F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) });
        body.Controls.Add(new Label { Text = "Your library is stored in:", AutoSize = true, ForeColor = Color.FromArgb(86, 99, 114), Margin = new Padding(0, 0, 0, 2) });
        var pathLabel = new Label { Text = libraryPath, AutoSize = true, ForeColor = Color.FromArgb(52, 94, 130), Margin = new Padding(0, 0, 0, 12), MaximumSize = new Size(400, 0) };
        body.Controls.Add(pathLabel);
        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Width = 84,
            Height = 32,
            BackColor = _brand,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 14, 0) };
        buttons.Controls.Add(ok);
        dialog.Controls.Add(body);
        dialog.Controls.Add(buttons);
        dialog.Controls.Add(banner);
        dialog.AcceptButton = ok;
        dialog.CancelButton = ok;
        dialog.ShowDialog(this);
    }

    private Control BuildHelpWorkspace()
    {
        var outer = Section("MPH Songs help", "A quick guide for service operators");
        var content = (TableLayoutPanel)outer.Tag!;
        content.RowCount = 2;
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var note = new Label
        {
            Text = "Use this tab during practice or before a service. All controls are safe to explore until you open the projector.",
            AutoSize = true,
            ForeColor = Color.FromArgb(52, 94, 130),
            BackColor = Color.FromArgb(232, 242, 251),
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 10)
        };
        var guide = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(35, 52, 72),
            Font = new Font("Segoe UI", 10.5F),
            Text = HelpText,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        content.Controls.Add(note, 0, 0);
        content.Controls.Add(guide, 0, 1);
        return outer;
    }

    private const string HelpText = """"
MPH SONGS - OPERATOR GUIDE

QUICK START
1. On the Text tab, select a song in the Song Library, or choose New song to create one (Ctrl+N).
2. Enter a title and lyrics. Leave a blank line between verses or chorus sections.
3. Press Ctrl+S or Save to keep the song in the library.
4. Click + Add current in the Service Agenda to add the song to your service order (Ctrl+Enter).
5. Select a slide, then press F5 or Project when the projector display is ready.

SONG LIBRARY
- New song: starts a blank song. Save: saves the current song.
- Delete: removes the current saved song. Agenda entries remain as lyric snapshots.
- Search filters the library; selecting a song opens it in the editor.
- A * in the window title means you have unsaved changes; you will be asked to save when you close the app.

SERVICE AGENDA
- + Add current: adds the song you are editing to the service order.
- Update: replaces the selected agenda item's title and lyrics with the current song.
- Remove: removes the selected agenda item. Up and Down change its position.
- Selecting an agenda item loads its saved lyric snapshot for review.

SLIDES AND VERSE NUMBERS
- A blank line starts the next verse. The slide list labels verses as V1, V2, and so on.
- If a verse needs more than the selected maximum lines, extra pages are labelled V1b, V1c, and so on.
- Press 1 through 9 (top-row numbers or numeric keypad) to go to the first slide for that verse.
- Use Left/Right, Up/Down, Page Up, or Page Down for the previous and next slides. Home and End jump to the start or end.

TEXT TAB
- Change the font, size, bold setting, text colour, and alignment from the ribbon.
- Max lines determines how many lyric lines fit on each slide.
- Tick Auto-fit to automatically scale the words so they fill the screen.
- Bullets and Numbers add or remove markers on the selected lines.
- Telugu fonts (Nirmala UI, Noto Sans Telugu, Gautami, and others) are available when installed.
- Ctrl+S saves, Ctrl+N starts a new song.

BACKGROUND TAB
- Choose a solid background colour or import an image or video.
- Imported files are copied into the MPH background library and shown as thumbnails; select one to use it.
- Delete removes the selected background from the library.
- Tick Loop video to repeat a video. Untick it to play once and hold the final frame.
- Adjust brightness for clear projected text.
- Pick landscape ratios 16:9, 4:3, or 16:10, portrait ratios 9:16 and 3:4, or Current to fill the screen.

BIBLE TAB
- + New Bible creates a translation. Import file accepts JSON or a Telugu SQLite database.
- In Verses, + New verse opens the verse editor. Select a verse and Edit or Delete to change it.
- Use Ctrl-click to select separate verses, or click and drag over the verse list to select a range.
- Selected verses become consecutive slides. + Add to agenda adds them to the service order.
- Jump to a reference with the box at the top: type e.g. "John 3:16" or "Psalm 23:1-6" and press Go or Enter.
- Show live projects the selected verses on the live display.

STAGE CONTROLS
- Black (F2): send a solid black screen to the projector between songs.
- Hide text (F3): keep the current background but remove the words.
- Logo (F4): show a church logo over the background. Set the logo on the Background tab.
- Press the same stage key again, or change slides, to return to the live text.

PROJECTOR
- F5 opens or closes the borderless projector window.
- MPH Songs uses the second monitor when one is connected; otherwise it uses the primary screen.
- Press Esc in the projector window to close it.

BACKUP
- On the Help tab, Export bundles the whole library into a .mphbundle file.
- Import restores a .mphbundle on another computer, replacing the current library.

CLOUD SYNC
- Songs can be shared with the MPH Songs web app (mph-songs.vercel.app) via MongoDB Atlas.
- Set URI on the Help ribbon to enter your Atlas connection string, then Check connection to verify.
- Disable turns cloud sync off and returns to the local library.
- If the connection fails, the app works fully offline with the local library.

SAVED DATA
Songs, agenda entries, and Bible records are saved to the local MPH Songs library on this computer. Back up this file before moving to another computer.
"""";
}
