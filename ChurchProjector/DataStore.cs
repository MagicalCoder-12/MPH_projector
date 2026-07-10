using System.Text.Json;

namespace ChurchProjector;

public sealed class AppData
{
    public List<Song> Songs { get; set; } = [];
    public List<AgendaItem> Agenda { get; set; } = [];
    public List<BibleTranslation> Bibles { get; set; } = [];
}

public sealed class Song
{
    public Song() { }
    public Song(string title, string lyrics) { Title = title; Lyrics = lyrics; }

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Lyrics { get; set; } = "";
    public override string ToString() => Title;
}

public sealed class AgendaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SongId { get; set; }
    public string Title { get; set; } = "";
    public string LyricsSnapshot { get; set; } = "";
    public override string ToString() => Title;
}

public sealed class BibleTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Bible";
    public List<BibleVerse> Verses { get; set; } = [];
    public override string ToString() => Name;
}

public sealed class BibleVerse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Book { get; set; } = "Genesis";
    public int Chapter { get; set; } = 1;
    public int Verse { get; set; } = 1;
    public string Text { get; set; } = "";
    public string Reference => $"{Book} {Chapter}:{Verse}";
    public override string ToString() => $"{Reference}  {Text}";
}

public sealed class LocalDataStore
{
    private readonly string _filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MPH Songs", "library.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public AppData Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new AppData();
            return JsonSerializer.Deserialize<AppData>(File.ReadAllText(_filePath), Options) ?? new AppData();
        }
        catch
        {
            return new AppData();
        }
    }

    public void Save(AppData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(data, Options));
    }

    public BibleTranslation ImportBible(string path)
    {
        var bible = JsonSerializer.Deserialize<BibleTranslation>(File.ReadAllText(path), Options)
                    ?? throw new InvalidDataException("The file does not contain a Bible translation.");
        bible.Id = Guid.NewGuid();
        bible.Name = string.IsNullOrWhiteSpace(bible.Name) ? Path.GetFileNameWithoutExtension(path) : bible.Name.Trim();
        bible.Verses ??= [];
        foreach (var verse in bible.Verses) verse.Id = Guid.NewGuid();
        return bible;
    }
}
