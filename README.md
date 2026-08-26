# MPH Songs

A Windows desktop song-projection application for church services.

## Features

1. **Song library** — create, open, edit, save, search, and delete songs. Songs persist in the local MPH Songs library, and unsaved edits are marked with a `*` in the window title and confirmed on exit.
2. **Service agenda** — build the order of service on the Song tab (`+ Add current`, Update, Remove, Up, Down). Each entry keeps its own lyric snapshot. Use **Ctrl+Enter** to add the current song.
3. **Automatic slides** — turn lyrics into slides; separate verses with blank lines. **1–9** jump straight to a verse, **Home/End** jump to the start/end, arrows and Page Up/Down page through slides.
4. **Text formatting ribbon** — font, size, bold/italic/underline/strike/sub/superscript, colour, alignment, line spacing, bullets and numbering, quick text styles, and **Auto-fit** text. Every control has a tooltip; the ribbon scrolls with the mouse wheel.
5. **Background ribbon** — solid colour presets or a custom colour, imported images and videos shown as a thumbnail library (select to use, Delete to remove), brightness, and aspect-ratio presets (16:9, 4:3, 16:10, 9:16, 3:4, or Current).
6. **Bible tab** — create/import translations (JSON or Telugu SQLite database), browse by book and chapter, jump to a reference (`John 3:16` or `Psalm 23:1-6`), create, edit, and delete verses in a verse editor, multi-select verses, preview them, and add them to the service agenda. **Show live** projects the selected passage.
7. **Projector** — borderless, top-most projector window using the second monitor when connected; keyboard stages (**F2** black, **F3** hide text, **F4** logo, **F5** projector, **Esc** close).
8. **Cloud sync** — optional MongoDB Atlas sync with the MPH Songs web app (60-second interval, manual *Sync now*, connection test, enable/disable from the Help tab). Fully functional offline.
9. **Backup & About** — export/import a `.mphbundle` of the whole library, and an About dialog with version and library location.

## Bible import format

Import a JSON file containing a translation name and its verses:

```json
{
  "name": "My Bible Translation",
  "verses": [
    { "book": "John", "chapter": 3, "verse": 16, "text": "Verse text goes here." }
  ]
}
```

## Run (development)

```powershell
dotnet run --project .\ChurchProjector\ChurchProjector.csproj
```

Or use `run.bat` for a menu-driven build/run launcher.

## Publish (production)

```bat
publish.bat
```

Builds a single-file `win-x64` release into the `publish\` folder — copy it to any Windows 10/11 machine.

## Keyboard shortcuts

| Key | Action |
| --- | --- |
| ↑ / ↓ / ← / →, Page Up / Page Down | Previous / next slide |
| 1–9 | Jump to song verse |
| Home / End | First / last slide |
| Ctrl+N / Ctrl+S | New song / Save song |
| Ctrl+Enter | Add current song to service agenda |
| F2 / F3 / F4 | Black screen / background only / logo |
| F5 | Open or close projector |
| Esc | Close projector |
