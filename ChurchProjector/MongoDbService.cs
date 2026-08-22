namespace ChurchProjector;

using MongoDB.Bson;
using MongoDB.Driver;

public class MongoDbService : IDisposable
{
    private readonly IMongoCollection<MongoSong> _songs;
    private readonly IMongoDatabase _database;
    private MongoClient? _client;

    public MongoDbService(string connectionString, string? databaseName = null)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        settings.ConnectTimeout = TimeSpan.FromSeconds(10);
        settings.SocketTimeout = TimeSpan.FromSeconds(30);
        _client = new MongoClient(settings);
        // Extract database name from URI if present (e.g. mongodb+srv://...@host/song_lyrics?...)
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            var mongoUrl = new MongoUrl(connectionString);
            databaseName = mongoUrl.DatabaseName;
        }
        _database = _client.GetDatabase(string.IsNullOrWhiteSpace(databaseName) ? "song_lyrics" : databaseName);
        _songs = _database.GetCollection<MongoSong>("songs");
    }

    public async Task<List<MongoSong>> GetAllSongsAsync()
    {
        return await _songs.Find(FilterDefinition<MongoSong>.Empty).ToListAsync();
    }

    public async Task<MongoSong?> GetSongByIdAsync(string id)
    {
        return await _songs.Find(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task<MongoSong> CreateSongAsync(MongoSong song)
    {
        await _songs.InsertOneAsync(song);
        return song;
    }

    public async Task UpdateSongAsync(string id, MongoSong song)
    {
        // Never overwrite web-owned metadata from desktop writes
        var updateFields = new List<UpdateDefinition<MongoSong>>();
        
        if (!string.IsNullOrEmpty(song.Title))
            updateFields.Add(Builders<MongoSong>.Update.Set(s => s.Title, song.Title));
        if (!string.IsNullOrEmpty(song.Lyrics))
            updateFields.Add(Builders<MongoSong>.Update.Set(s => s.Lyrics, song.Lyrics));
        updateFields.Add(Builders<MongoSong>.Update.Set(s => s.SongLanguage, song.SongLanguage));
        updateFields.Add(Builders<MongoSong>.Update.Set(s => s.IsChoirPractice, song.IsChoirPractice));
        updateFields.Add(Builders<MongoSong>.Update.Set(s => s.IsChristmasSong, song.IsChristmasSong));
        updateFields.Add(Builders<MongoSong>.Update.Set(s => s.Tags, song.Tags));
        updateFields.Add(Builders<MongoSong>.Update.Set(s => s.UpdatedAt, DateTime.UtcNow));
        
        // Preserve web fields; only set Desktop/Desktop-specific metadata
        updateFields.Add(Builders<MongoSong>.Update.Set(s => s.Desktop, song.Desktop));
        updateFields.Add(Builders<MongoSong>.Update.Set(s => s.Source, song.Source));
        // Do NOT update Web here — that is web-dominant metadata

        var update = Builders<MongoSong>.Update.Combine(updateFields);
        await _songs.UpdateOneAsync(s => s.Id == id, update);
    }

    public async Task DeleteSongAsync(string id)
    {
        await _songs.DeleteOneAsync(s => s.Id == id);
    }

    public void Dispose()
    {
        // MongoDB.Driver does not require explicit client disposal for normal operation.
        // This satisfies IDisposable for potential future use.
        GC.SuppressFinalize(this);
    }
}
