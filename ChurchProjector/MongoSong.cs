namespace ChurchProjector;

public class MongoSong
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string SongLanguage { get; set; } = "Telugu";
    public string Lyrics { get; set; } = string.Empty;
    public bool IsChoirPractice { get; set; }
    public bool IsChristmasSong { get; set; }
    public List<string> Tags { get; set; } = ["church"];
    public string? Web { get; set; }
    public string? Desktop { get; set; }
    public string Source { get; set; } = "desktop";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
