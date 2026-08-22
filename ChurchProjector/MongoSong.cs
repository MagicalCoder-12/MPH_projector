using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChurchProjector;

[BsonIgnoreExtraElements]
public class MongoSong
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("subtitle")]
    public string? Subtitle { get; set; }

    [BsonElement("songLanguage")]
    public string SongLanguage { get; set; } = "Telugu";

    [BsonElement("lyrics")]
    public string Lyrics { get; set; } = string.Empty;

    [BsonElement("isChoirPractice")]
    public bool IsChoirPractice { get; set; }

    [BsonElement("isChristmasSong")]
    public bool IsChristmasSong { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = ["church"];

    [BsonElement("web")]
    public string? Web { get; set; }

    [BsonElement("desktop")]
    public string? Desktop { get; set; }

    [BsonElement("source")]
    public string Source { get; set; } = "desktop";

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
