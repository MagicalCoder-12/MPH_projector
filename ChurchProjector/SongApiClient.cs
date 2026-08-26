using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChurchProjector;

public sealed class SongApiClient : IDisposable
{
    private const string BaseUrl = "https://mph-songs.vercel.app";
    private const string DesktopSecret = "mph-desktop-sync-2026";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(BaseUrl),
        Timeout = TimeSpan.FromSeconds(15),
    };

    public async Task<List<ApiSong>> GetAllSongsAsync()
    {
        var response = await _http.GetAsync("/api/songs");
        response.EnsureSuccessStatusCode();
        var wrapper = await response.Content.ReadFromJsonAsync<ApiSongResponse>(JsonOptions);
        return wrapper?.Songs ?? [];
    }

    public async Task<ApiSong?> CreateSongAsync(ApiSong song)
    {
        var response = await _http.PostAsJsonAsync("/api/songs", song, JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiCreateResponse>(JsonOptions);
        return result?.NewSong;
    }

    public async Task<ApiSong?> UpdateSongAsync(string id, ApiSong song)
    {
        var response = await _http.PutAsJsonAsync($"/api/songs/{id}", song, JsonOptions);
        if ((int)response.StatusCode == 409)
        {
            var conflict = await response.Content.ReadFromJsonAsync<ConflictResponse>(JsonOptions);
            throw new ConflictException(conflict?.ServerSong, conflict?.Error ?? "Conflict");
        }
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiUpdateResponse>(JsonOptions);
        return result?.UpdatedSong;
    }

    public async Task<bool> DeleteSongAsync(string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/songs/desktop-delete")
        {
            Content = JsonContent.Create(new { id }, options: JsonOptions)
        };
        request.Headers.Add("X-Desktop-Secret", DesktopSecret);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}

// --- API DTOs matching the Next.js API response shape ---

public class ApiSongResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("songs")]
    public List<ApiSong> Songs { get; set; } = [];
}

public class ApiCreateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("newSong")]
    public ApiSong? NewSong { get; set; }
}

public class ApiUpdateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("updatedSong")]
    public ApiSong? UpdatedSong { get; set; }
}

public class ApiSong
{
    [JsonPropertyName("_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("songLanguage")]
    public string SongLanguage { get; set; } = "Telugu";

    [JsonPropertyName("lyrics")]
    public string Lyrics { get; set; } = "";

    [JsonPropertyName("isChoirPractice")]
    public bool IsChoirPractice { get; set; }

    [JsonPropertyName("isChristmasSong")]
    public bool IsChristmasSong { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = ["church"];

    [JsonPropertyName("source")]
    public string Source { get; set; } = "desktop";

    [JsonPropertyName("desktop")]
    public string? Desktop { get; set; }

    [JsonPropertyName("owner")]
    public string Owner { get; set; } = "web";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("expectedUpdatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ExpectedUpdatedAt { get; set; }
}

public class ConflictResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("serverSong")] public ApiSong? ServerSong { get; set; }
}

public class ConflictException : Exception
{
    public ApiSong? ServerSong { get; }
    public ConflictException(ApiSong? serverSong, string message) : base(message) { ServerSong = serverSong; }
}
