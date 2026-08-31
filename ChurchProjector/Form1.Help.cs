namespace ChurchProjector;

public partial class Form1
{

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
            ForeColor = Color.FromArgb(37, 66, 143),
            BackColor = Color.FromArgb(232, 238, 255),
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 0, 0, 10)
        };
        var guide = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(52, 64, 84),
            Font = new Font("Segoe UI", 10.5F),
            Text = HelpText,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        content.Controls.Add(note, 0, 0);
        content.Controls.Add(guide, 0, 1);
        return outer;
    }

    private const string HelpText = """
MPH SONGS - OPERATOR GUIDE

QUICK START
1. On the Text tab, double-click a song in the Song Library or choose + New to create one.
2. Enter a title and lyrics. Leave a blank line between verses or chorus sections.
3. Click Save to keep the song in the library.
4. Click + Add in Service Agenda to add the current song to your service order.
5. Select a slide, then press F5 or Open projector when the projector display is ready.

SONG LIBRARY
- New: starts a blank song.
- Save: creates a new song or updates the song you opened.
- Delete: removes the current saved song. Agenda entries remain as lyric snapshots.
- Search: filters the Song Library. Double-click a result to open it.

SERVICE AGENDA
- + Add: adds the current song to the service order.
- Update: replaces the selected agenda item's title and lyrics with the current song.
- Remove: removes the selected agenda item.
- Up and Down: change the order of the selected agenda item.
- Click an agenda item to load its saved lyric snapshot.

SLIDES AND VERSE NUMBERS
- A blank line starts the next verse. The slide list labels verses as V1, V2, and so on.
- If a verse needs more than the selected maximum lines, extra pages are labelled V1b, V1c, and so on.
- Press 1 through 9 (top-row numbers or numeric keypad) to go to the first slide for that verse.
- Use Left/Right, Up/Down, Page Up, or Page Down for the previous and next slides.

TEXT TAB
- Change the font, size, bold setting, text colour, and alignment.
- Max. lines determines how many lyric lines fit on each slide.
- Tick Auto-fit text to automatically scale and wrap the words so they fill the screen.
- Telugu fonts (Nirmala UI, Noto Sans Telugu, Gautami, and others) are available in the font list when installed.

BACKGROUND TAB
- Choose a solid background colour, import a custom image, or import a video.
- Imported images and videos are copied into the MPH Songs background library, so they remain available after restarting the app.
- Select a saved item from the background list to use it again. Clear returns to a solid colour.
- Tick Loop video to repeat a video continuously. Untick it to let the video play once and hold its final frame.
- Adjust brightness for clear projected text.
- Pick landscape ratios 16:9, 4:3, or 16:10, or portrait ratios 9:16 and 3:4.
- Pick Current to fill the whole projector screen with the selected image or video, ignoring aspect ratio.

BIBLE TAB
- New Bible creates a Bible translation record. Save Bible stores its name even before verses are added.
- Enter a Book, Chapter, Verse, and verse text, then choose Save verse.
- Use Ctrl-click to select separate verses, or click and drag over the verse list to select a continuous range.
- Selected verses become consecutive live slides. Add verse adds the selected passage to the service agenda.
- Jump to a reference with the box at the top: type e.g. "John 3:16" or "Psalm 23:1-6" and press Go or Enter.
- The Service agenda on the right holds the Bible passages for this service. Use Add verse in the Verses panel, then Remove/Up/Down here. Number keys do not jump verses in this tab.
- Drag the separators between the books, verses, preview, and agenda panels to give each area more room.
- Import accepts a JSON translation file. See the project README for its required format.

STAGE CONTROLS
- Black (F2): send a solid black screen to the projector between songs.
- Hide text (F3): keep the current background but remove the words.
- Logo (F4): show a church logo over the background. Set the logo on the Background tab (Set logo).
- These work whether or not the projector is already open; the status bar shows the current stage.
- Press the same stage key again, or change slides, to return to the live text.

PROJECTOR
- F5 opens or closes the borderless projector window.
- MPH Songs uses the second monitor when one is connected; otherwise it uses the primary screen.
- Press Esc in the projector window to close it.

BACKUP
- On the Help tab, Export bundles the whole library (songs, Bibles, agenda, backgrounds, and logo) into a .mphbundle file.
- Import restores a .mphbundle on another computer, replacing the current library.

CLOUD SYNC
- Songs sync automatically between this app and the web app (mph-songs.vercel.app).
- Songs created in the projector appear on the web app within 60 seconds, and vice versa.
- Click Refresh now on the Help or Text ribbon for an immediate sync.
- If the network is unavailable, the app works fully offline with the local library.

SAVED DATA
Songs, agenda entries, and Bible records are saved to the local MPH Songs library on this computer. Back up this file before moving to another computer.
""";
}
