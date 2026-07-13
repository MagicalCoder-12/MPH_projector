using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace ChurchProjector;

public sealed class AppData
{
    public List<Song> Songs { get; set; } = [];
    public List<AgendaItem> Agenda { get; set; } = [];
    public List<BibleTranslation> Bibles { get; set; } = [];
    public List<BackgroundAsset> Backgrounds { get; set; } = [];
    public BackgroundPreferences BackgroundPreferences { get; set; } = new();
}

public sealed class BackgroundAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "Image";
    public string FilePath { get; set; } = "";
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public override string ToString() => $"[{Kind}] {Name}";
}

public sealed class BackgroundPreferences
{
    public Guid? SelectedBackgroundId { get; set; }
    public int BackgroundColorArgb { get; set; } = unchecked((int)0xFF162234);
    public int Brightness { get; set; }
    public string AspectRatio { get; set; } = "16:9";
    public bool VideoLoop { get; set; } = true;
    public string? LogoPath { get; set; }
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
    public override string ToString() => $"{Verse}    {Text}";
}

public sealed class LocalDataStore
{
    private readonly string _filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MPH Songs", "library.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private string BackgroundRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MPH_projector");
    private string ImageDirectory => Path.Combine(BackgroundRoot, "images");
    private string VideoDirectory => Path.Combine(BackgroundRoot, "videos");

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

    public string Serialize(AppData data) => JsonSerializer.Serialize(data, Options);

    public AppData? Deserialize(string json) => JsonSerializer.Deserialize<AppData>(json, Options);

    public string ImportLogo(string sourcePath)
    {
        if (!File.Exists(sourcePath)) return "";
        var directory = Path.Combine(BackgroundRoot, "logos");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, $"{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}");
        File.Copy(sourcePath, destination, true);
        return destination;
    }

    public BibleTranslation ImportBible(string path)
    {
        if (string.Equals(Path.GetExtension(path), ".vpc", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("This VideoPsalm VPC package is encrypted. Export or obtain the Bible as an unprotected JSON file before importing it into MPH Songs.");
        var bible = JsonSerializer.Deserialize<BibleTranslation>(File.ReadAllText(path), Options)
                    ?? throw new InvalidDataException("The file does not contain a Bible translation.");
        bible.Id = Guid.NewGuid();
        bible.Name = string.IsNullOrWhiteSpace(bible.Name) ? Path.GetFileNameWithoutExtension(path) : bible.Name.Trim();
        bible.Verses ??= [];
        foreach (var verse in bible.Verses) verse.Id = Guid.NewGuid();
        return bible;
    }

    public BibleTranslation ImportSqliteBible(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("The selected Bible database was not found.", path);
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        connection.Open();

        using var configuration = connection.CreateCommand();
        configuration.CommandText = "SELECT title, booknames FROM configuration LIMIT 1";
        using var configurationReader = configuration.ExecuteReader();
        if (!configurationReader.Read()) throw new InvalidDataException("The database has no Bible configuration record.");
        var title = configurationReader.IsDBNull(0) ? Path.GetFileNameWithoutExtension(path) : configurationReader.GetString(0).Trim();
        var rawBookNames = configurationReader.IsDBNull(1) ? "" : configurationReader.GetString(1);
        var bookNames = JsonSerializer.Deserialize<string[]>($"[{rawBookNames}]") ?? [];
        if (bookNames.Length == 0) throw new InvalidDataException("The database has no usable book names.");

        var bible = new BibleTranslation { Name = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(path) : title };
        using var verses = connection.CreateCommand();
        verses.CommandText = "SELECT word, bookNum, chNum, verseNum FROM words ORDER BY bookNum, chNum, verseNum";
        using var reader = verses.ExecuteReader();
        while (reader.Read())
        {
            var bookNumber = reader.GetInt32(1);
            if (bookNumber < 1 || bookNumber > bookNames.Length) continue;
            bible.Verses.Add(new BibleVerse
            {
                Book = bookNames[bookNumber - 1],
                Chapter = reader.GetInt32(2),
                Verse = reader.GetInt32(3),
                Text = reader.IsDBNull(0) ? "" : reader.GetString(0)
            });
        }
        if (bible.Verses.Count == 0) throw new InvalidDataException("The database did not contain readable Bible verses.");
        return bible;
    }

    public BackgroundAsset ImportBackground(string path, string kind)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("The selected background file was not found.", path);
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        var videoExtensions = new[] { ".mp4", ".wmv", ".avi", ".mov", ".m4v" };
        if (kind == "Image" && !imageExtensions.Contains(extension)) throw new InvalidDataException("Choose a supported image file.");
        if (kind == "Video" && !videoExtensions.Contains(extension)) throw new InvalidDataException("Choose a supported video file.");

        var destinationDirectory = kind == "Video" ? VideoDirectory : ImageDirectory;
        Directory.CreateDirectory(destinationDirectory);
        var sourcePath = Path.GetFullPath(path);
        var destination = string.Equals(Path.GetDirectoryName(sourcePath), destinationDirectory, StringComparison.OrdinalIgnoreCase)
            ? sourcePath
            : Path.Combine(destinationDirectory, $"{Guid.NewGuid():N}{extension}");
        if (!string.Equals(sourcePath, destination, StringComparison.OrdinalIgnoreCase)) File.Copy(sourcePath, destination, false);
        return new BackgroundAsset
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Kind = kind,
            FilePath = destination
        };
    }

    public IEnumerable<BackgroundAsset> DiscoverBackgroundAssets()
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        var videoExtensions = new[] { ".mp4", ".wmv", ".avi", ".mov", ".m4v" };
        foreach (var item in Discover(ImageDirectory, "Image", imageExtensions)) yield return item;
        foreach (var item in Discover(VideoDirectory, "Video", videoExtensions)) yield return item;
    }

    private static IEnumerable<BackgroundAsset> Discover(string directory, string kind, string[] extensions)
    {
        if (!Directory.Exists(directory)) yield break;
        foreach (var file in Directory.EnumerateFiles(directory).Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)))
        {
            yield return new BackgroundAsset { Name = Path.GetFileNameWithoutExtension(file), Kind = kind, FilePath = file };
        }
    }
}
