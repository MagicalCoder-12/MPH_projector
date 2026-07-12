namespace ChurchProjector;

public partial class Form1
{
    private Control BuildHelpRibbon()
    {
        var ribbon = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        var start = RibbonGroup("Getting started", 355);
        Add(start, Hint("1. Open or create a song.  2. Add it to the service agenda.  3. Choose a slide.  4. Press F5 to project."));
        var keyboard = RibbonGroup("Keyboard shortcuts", 330);
        Add(keyboard, Hint("Arrows or Page Up/Page Down: slides.  1-9: jump to song verses.  F5: projector.  Esc: close projector."));
        var storage = RibbonGroup("Library", 275);
        Add(storage, Hint("Songs, agenda items, Bible translations, and verses are saved automatically on this computer."));
        ribbon.Controls.AddRange([start, keyboard, storage]);
        return ribbon;
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
- Use the Quick style buttons for readable light, warm, or dark text styles.

BACKGROUND TAB
- Choose a solid background colour, import a custom image, or import a video.
- Imported images and videos are copied into the MPH Songs background library, so they remain available after restarting the app.
- Select a saved item from the background list to use it again. Clear returns to a solid colour.
- Tick Loop video to repeat a video continuously. Untick it to let the video play once and hold its final frame.
- Adjust brightness for clear projected text.
- Pick landscape ratios 16:9, 4:3, or 16:10, or portrait ratios 9:16 and 3:4.

BIBLE TAB
- New Bible creates a Bible translation record. Save Bible stores its name even before verses are added.
- Enter a Book, Chapter, Verse, and verse text, then choose Save verse.
- Use Ctrl-click to select separate verses, or click and drag over the verse list to select a continuous range.
- Selected verses become consecutive live slides. Add verse adds the selected passage to the service agenda.
- Drag the separators between the agenda, books, verses, and preview panels to give each area more room.
- Import accepts a JSON translation file. See the project README for its required format.

PROJECTOR
- F5 opens or closes the borderless projector window.
- MPH Songs uses the second monitor when one is connected; otherwise it uses the primary screen.
- Press Esc in the projector window to close it.

SAVED DATA
Songs, agenda entries, and Bible records are saved to the local MPH Songs library on this computer. Back up this file before moving to another computer.
""";
}
