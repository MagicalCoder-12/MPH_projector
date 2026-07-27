# MongoDB Direct Integration Plan

> For Hermes: Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Connect MPH Songs WinForms projector directly to MongoDB Atlas so songs are shared with the MPH Songs Next.js web app in real time. No sync layer needed — both apps write to the same Mongo collection.

**Architecture:**
```
MongoDB Atlas (single source of truth)
  /              \
Website (Next.js)     Projector (C#)
reads songs          reads + writes songs
```

The web app reads from Atlas (existing). The projector adds a direct Mongo connection and writes to the same `songs` collection. Web app auto-sees new songs because it queries Atlas live.

**Key insights:**
- Songs saved in the projector get `tags: ["church"]` for easy filtering on the website
- Songs saved in the web app get `tags: ["web"]` for easy filtering on the projector
- Songs also get a `source` field (`"web"` or `"desktop"`) so you can query or display the origin clearly
- When a song is deleted in either app, it vanishes from both (same collection, same delete operation)

---

## Tech Stack

- **C# WinForms** projector (D:\programs\C#\MPH_projector)
- **MongoDB.Driver** NuGet package for direct Mongo connection
- **MongoDB Atlas** cluster (existing, same URI the web app uses)
- **Shared collection:** `songs` in the same database

---

## Background context

- Web app (D:\programs\Node\MPHSongs) uses Mongoose with `MONGODB_URI` env var pointing to Atlas
- Song schema has: `title`, `subtitle`, `songLanguage`, `lyrics`, `isChoirPractice`, `isChristmasSong`, `tags`, `createdAt`, `updatedAt`
- Web app REST API at `/api/songs` (GET/POST/PUT/DELETE) — projector will NOT use this for writes, direct Mongo only
- Projector currently uses local `library.json` (~25MB) for storage
- `MONGODB_URI` stored in `.env` at D:\programs\Node\MPHSongs\.env

---

## Step-by-step plan

### Task 1: Add MongoDB package to C# project

**Objective:** Add MongoDB.Driver NuGet package to the projector project.

**Files:**
- Modify: `ChurchProjector/ChurchProjector.csproj`

**Steps:**
```bash
cd /d/programs/C#/MPH_projector
dotnet add ChurchProjector/ChurchProjector.csproj package MongoDB.Driver --version 2.19.0
```

**Verification:** `dotnet build ChurchProjector/ChurchProjector.csproj` succeeds with package reference visible.

---

### Task 2: Create `MongoDbService` class

**Objective:** New file that manages the Mongo connection and CRUD operations.

**Files:**
- Create: `ChurchProjector/MongoDbService.cs`

**Details:**
- Class: `MongoDbService` (implements `IDisposable`)
- Constructor takes connection string from settings/env var
- Uses `MongoClient` → `IMongoDatabase` → `IMongoCollection<Song>`
- Key methods:

```csharp
public class MongoDbService : IDisposable
{
    private readonly IMongoCollection<MongoSong> _songs;

    public MongoDbService(string connectionString, string databaseName = "mphongs")
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(databaseName);
        _songs = db.GetCollection<MongoSong>("songs");
    }

    public async Task<List<MongoSong>> GetAllSongsAsync() { ... }
    public async Task<MongoSong> GetSongByIdAsync(string id) { ... }
    public async Task CreateSongAsync(MongoSong song) { ... }
    public async Task UpdateSongAsync(string id, MongoSong song) { ... }
    public async Task DeleteSongAsync(string id) { ... }

    public void Dispose() { ... }
}
```

**`MongoSong` model** maps to web app's Mongoose schema:
```csharp
public class MongoSong
{
    public string Id { get; set; } = string.Empty;      // MongoDB _id
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string SongLanguage { get; set; } = "Telugu";
    public string Lyrics { get; set; } = string.Empty;
    public bool IsChoirPractice { get; set; }
    public bool IsChristmasSong { get; set; }
    public List<string> Tags { get; set; } = ["church"];
    public string? Web { get; set; }       // null or "true" — set when created via web app
    public string? Desktop { get; set; }    // null or "true" — set when created via projector
    public string Source { get; set; } = "desktop";     // "desktop" or "web"
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Field convention:**
- Projector creates songs with `Tags = ["church"]`, `Desktop = "true"`, `Source = "desktop"` — web app can filter by `church` tag
- Web app creates songs with `Web = "true"`, `Source = "web"` — projector ignores web tags
- `tags` array carries the projector's `church` tag for web app filtering; web app manages its own tags independently

**Verification:**
- Build passes (no compilation errors)
- `MongoSong` class compiles with all required fields

---

### Task 3: Read `MONGODB_URI` from configuration

**Objective:** The projector needs the Atlas connection string. Don't hardcode it.

**Files:**
- Modify: `ChurchProjector/DataStore.cs` (add a settings field)
- Create: `ChurchProjector/AppConfig.cs` (or add to existing settings class)

**Details:**
- Add `MongoDbConnectionString` property to the existing `AppData` settings class
- Default: read from environment variable `MPH_MONGODB_URI`
- If not set, the projector falls back to local `library.json` mode (offline, same as before)
- Store it in `%APPDATA%/MPH_projector/settings.json` alongside other config

```csharp
public class AppConfig
{
    public string MongoDbConnectionString { get; set; } =
        Environment.GetEnvironmentVariable("MPH_MONGODB_URI") ?? string.Empty;
    public bool UseMongoDb => !string.IsNullOrEmpty(MongoDbConnectionString);
}
```

**Verification:** Build passes, env var fallback works.

---

### Task 4: Wire Mongo service into DataStore

**Objective:** Add `MongoDbService` as a member of `AppData` and initialize it on startup.

**Files:**
- Modify: `ChurchProjector/DataStore.cs`
- Modify: `ChurchProjector/Form1.cs` (constructor)

**Details:**
- In `AppData` constructor or a new `InitMongoAsync()` method:
  - If `AppConfig.UseMongoDb` is true, create `MongoDbService` instance
  - Fetch all songs from Mongo and populate `_library`
  - If Mongo is unavailable, fall back to local `library.json`
- Keep `Persist()` (existing `library.json` writer) as a local cache, not the source of truth when Mongo is active

**Offline behavior:** If Mongo connection fails at startup, use local `library.json` and show a status bar indicator "Local mode — no cloud connection".

**Verification:**
- Build passes
- With no `MPH_MONGODB_URI`, falls back to local mode seamlessly
- With `MPH_MONGODB_URI` set and Atlas reachable, loads songs from Mongo

---

### Task 5: Write-through sync on save/create/delete

**Objective:** When a song is saved, created, or deleted in the projector, also write it to MongoDB Atlas.

**Files:**
- Modify: `ChurchProjector/Form1.Crud.cs` — `SaveCurrentSong()`, `NewSong()`, `DeleteCurrentSong()`

**Details:**

**SaveCurrentSong():**
After local `Persist()`:
```csharp
if (_data.UseMongoDb && _data.MongoDb is not null)
{
    if (song.SourceId is null)
    {
        // New song → create in Mongo
        var mongoSong = SongToMongo(song);
        mongoSong.Desktop = "true";   // Mark as created by projector
        mongoSong.Source = "desktop";
        var result = await _data.MongoDb.CreateSongAsync(mongoSong);
        song.SourceId = result.InsertedId; // store web app _id
    }
    else
    {
        // Existing song → update in Mongo
        var mongoSong = SongToMongo(song);
        await _data.MongoDb.UpdateSongAsync(song.SourceId, mongoSong);
    }
}
```

**DeleteCurrentSong():**
After local removal:
```csharp
if (_data.UseMongoDb && song.SourceId is not null)
{
    await _data.MongoDb.DeleteSongAsync(song.SourceId);
}
```

Since both apps hit the same collection, deleting a song removes it everywhere instantly — the web app's next fetch picks up the absence. No sync event needed.

**`SongToMongo()` helper:** Maps C# `Song` → `MongoSong`, preserving lyrics, title, language, etc.

**Verification:**
- Create a song in projector → check it appears in Mongo Atlas → check it appears on the web app
- Edit a song in projector → check the web app reflects the change
- Delete a song in projector → check it disappears from the web app

---

### Task 6: Add periodic sync refresh

**Objective:** Songs created in the web app should appear in the projector without restarting.

**Files:**
- Modify: `ChurchProjector/DataStore.cs`
- Modify: `ChurchProjector/Form1.cs`

**Details:**
- Add a `Timer` that fires every 60 seconds
- On tick, call `MongoDb.GetAllSongsAsync()` and merge remote songs into local library
- Skip songs already in local library (matched by `SourceId`)
- Newly appeared songs (created on the web app side) get absorbed into local library
- Update the `_libraryList` UI to show new songs

**UI indicator:** In the status bar, show `● Synced` (green dot) after each successful sync.

**Verification:**
- Start projector → add song in web app → confirm projector picks it up within 60 seconds (or on manual refresh)

---

### Task 7: Handle MongoDB connection errors gracefully

**Objective:** Projector stays fully functional even if Mongo is unreachable.

**Files:**
- Modify: `ChurchProjector/DataStore.cs`
- Modify: `ChurchProjector/Form1.cs`

**Details:**
- All Mongo calls wrapped in `try/catch (MongoException)`
- On fetch failure: use local cache, status bar shows "Offline — local library"
- On write failure: keep local save (don't lose user data), status bar shows "Sync failed — saved locally, will retry"
- Retry logic: exponential backoff up to 3 attempts, then fall back to local mode

**Verification:**
- Disconnect from network → projector still works → status bar shows offline
- Reconnect → next sync cycle picks up normally

---

## Files likely to change

```
ChurchProjector/ChurchProjector.csproj      — add MongoDB.Driver package
ChurchProjector/MongoDbService.cs             — NEW: Mongo CRUD service
ChurchProjector/MongoSong.cs                  — NEW: Mongo song model
ChurchProjector/DataStore.cs                  — add Mongo init, config, sync timer
ChurchProjector/Form1.cs                      — add sync timer, status indicator
ChurchProjector/Form1.Crud.cs                 — write-through on save/delete/create
```

**Web app side change (minor):** The web app's Mongoose schema should add `source`, `web`, and `desktop` fields. This is a one-line schema update in `src/lib/models/Song.ts`. The projector sets `Desktop = "true"` and `Source = "desktop"` on all songs it creates. The web app sets `Web = "true"` and `Source = "web"` on songs it creates (existing songs may need a one-time backfill).

---

## Testing checklist

1. `dotnet build ChurchProjector/ChurchProjector.csproj` — green
2. `dotnet run` with no `MPH_MONGODB_URI` — works in local offline mode (existing behavior preserved)
3. Set `MPH_MONGODB_URI` env var → restart projector → songs load from Atlas
4. Save a new song in projector → appears in atlas with `source: "desktop"` and `desktop: "true"` → appears on web app
5. Add a song in web app with `source: "web"` and `web: "true"` → appears in projector within 60s
6. Delete a song in projector → vanishes from atlas → vanishes from web app
7. Delete a song in web app → vanishes from atlas → vanishes from projector within 60s
8. Disconnect from network → projector works offline with local cache
9. Reconnect → sync resumes, new songs from website appear

---

## Risks

- **Schema mismatch:** Web app has `isGoodFridaySong`, `isChurchSong`, `isYouthSong`, `isSundaySchoolSong` boolean flags that projector Song model doesn't track. For MVP, ignore these. The projector just needs `title`, `lyrics`, `songLanguage`, `isChoirPractice`.
- **New fields for origin tracking:** `desktop`, `web`, and `source` fields added to the Mongo schema. The projector sets `desktop = "true"` + `source = "desktop"`. The web app sets `web = "true"` + `source = "web"`. These fields are purely for identifying origin — they don't affect existing functionality.
- **Existing songs have no origin field:** Songs created before this integration have no `desktop`/`web`/`source` fields (null). The projector should treat untagged songs as "unknown origin" and can optionally tag them as `desktop` on first edit.
- **ID mapping:** `SourceId` stores the web app's MongoDB `_id`. The projector uses its own `Guid` internally — no collision.
- **Large library (25MB+):** Fetching all songs on startup might be slow. Consider adding a `limit` parameter to the initial fetch (e.g., latest 500 songs) or only fetching songs needed for the current session.
- **Offline writes:** If user edits a song while offline, the write to Mongo fails silently (kept in local). On reconnect, the local version wins. If someone else edited the same song on the web app, the projector's overwrite would win (last-write-wins). Acceptable for MVP.
