# MPH Songs

A focused Windows song-projection application for church services.

## Current features

1. Create, open, edit, save, and delete songs. Songs and agenda items persist in the local MPH Songs library.
2. Add, update, remove, and reorder service-agenda entries. Each entry keeps its own lyric snapshot.
3. Turn song lyrics into slides automatically. Separate verses with blank lines; press **1** through **9** to jump straight to that verse.
4. Use the **Text** and **Background** ribbons to format the live slide.
5. Use the **Bible** tab to create/delete Bible translations, create/edit/delete verses, import a Bible JSON file, preview a verse, and project it.
6. Present slides in a borderless projector window, using the second monitor when connected.

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

## Run

```powershell
dotnet run --project .\ChurchProjector\ChurchProjector.csproj
```

Use **Up/Down**, **Left/Right**, or **Page Up/Page Down** to change slides. **1-9** jump to song verses. **F5** opens or closes the projector; **Esc** closes its projector window.
