namespace ChurchProjector;

public partial class Form1
{
    private async void SaveCurrentSong()
    {
        if (!SaveCurrentSongCore()) return;
        await PushSavedSongToCloudAsync();
    }

    /// <summary>
    /// Saves the current title and lyrics to the local library.
    /// Returns false (and tells the user) when the song cannot be saved.
    /// </summary>
    private bool SaveCurrentSongCore()
    {
        var title = _titleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show(this, "Enter a song title before saving.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _titleBox.Focus();
            return false;
        }

        var song = _currentSongId is Guid id ? _library.FirstOrDefault(item => item.Id == id) : null;
        if (song is null)
        {
            song = new Song();
            _library.Add(song);
            _currentSongId = song.Id;
        }
        song.Title = title;
        song.Lyrics = _lyricsBox.Text;
        Persist();
        FilterLibrary(_librarySearch?.Text ?? string.Empty);
        if (_libraryList is not null) _libraryList.SelectedItem = song;
        _slideStatus.Text = "Saved - " + song.Title;
        MarkDirty(false);
        return true;
    }

    private async Task PushSavedSongToCloudAsync()
    {
        if (!_data.Sync.UseMongoDb || _mongoDb is null) return;
        var song = _currentSongId is Guid id ? _library.FirstOrDefault(item => item.Id == id) : null;
        if (song is null) return;
        try
        {
            var mongoSong = ToMongoSong(song);
            if (song.SourceId is null)
            {
                mongoSong.Tags = ["church"];
                mongoSong.Desktop = "true";
                mongoSong.Source = "desktop";
                var created = await _mongoDb.CreateSongAsync(mongoSong);
                song.SourceId = created.Id;
                Persist();
            }
            else
            {
                var existing = await _mongoDb.GetSongByIdAsync(song.SourceId);
                if (existing is not null && string.Equals(existing.Source, "web", StringComparison.OrdinalIgnoreCase))
                {
                    _slideStatus.Text = "Saved locally only (web-owned)";
                }
                else
                {
                    await _mongoDb.UpdateSongAsync(song.SourceId, mongoSong);
                }
            }
        }
        catch
        {
            // ignore sync failure; local save already succeeded
        }
    }

    private async void DeleteCurrentSong()
    {
        var song = _currentSongId is Guid id ? _library.FirstOrDefault(item => item.Id == id) : null;
        if (song is null)
        {
            NewSong();
            return;
        }
        if (MessageBox.Show(this, $"Delete '{song.Title}' from the Song Library?\n\nAgenda entries are kept as snapshots.", "MPH Songs", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _library.Remove(song);
        Persist();
        FilterLibrary(_librarySearch?.Text ?? string.Empty);
        NewSong();

        if (_data.Sync.UseMongoDb && _mongoDb is not null && song.SourceId is not null)
        {
            try
            {
                await _mongoDb.DeleteSongAsync(song.SourceId);
            }
            catch
            {
                // ignore sync failure; local delete already succeeded
            }
        }
    }

    private void NewSong()
    {
        LoadSongContent("", "", null, "New song");
        MarkDirty(true);
        _titleBox.Focus();
    }

    private static MongoSong ToMongoSong(Song song) => new()
    {
        Id = song.SourceId ?? string.Empty,
        Title = song.Title,
        Lyrics = song.Lyrics,
        SongLanguage = "Telugu",
        IsChoirPractice = false,
        IsChristmasSong = false,
        Tags = ["church"],
        Web = null,
        Desktop = "true",
        Source = "desktop",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private void RefreshAgenda()
    {
        if (_agendaList is not null)
        {
            _updating = true;
            _agendaList.BeginUpdate();
            _agendaList.Items.Clear();
            foreach (var item in _agenda) _agendaList.Items.Add(item);
            _agendaList.EndUpdate();
            _updating = false;
        }
        RefreshBibleAgenda();
    }

    private void LoadAgendaItem()
    {
        if (_activeAgendaList?.SelectedItem is not AgendaItem item) return;
        LoadSongContent(item.Title, item.LyricsSnapshot, item.SongId, "Loaded from Service Agenda");
    }

    private void RemoveAgendaItem()
    {
        if (_activeAgendaList?.SelectedItem is not AgendaItem item) return;
        var index = _activeAgendaList.SelectedIndex;
        _agenda.Remove(item);
        Persist();
        RefreshAgenda();
        if (_agenda.Count > 0) _activeAgendaList.SelectedIndex = Math.Min(index, _agenda.Count - 1);
    }

    private void UpdateAgendaItem()
    {
        if (_activeAgendaList?.SelectedItem is not AgendaItem item) return;
        item.Title = string.IsNullOrWhiteSpace(_titleBox.Text) ? "Untitled song" : _titleBox.Text.Trim();
        item.SongId = _currentSongId;
        item.LyricsSnapshot = _lyricsBox.Text;
        var index = _activeAgendaList.SelectedIndex;
        Persist();
        RefreshAgenda();
        _activeAgendaList.SelectedIndex = index;
    }

    private void MoveAgendaItem(int direction)
    {
        if (_activeAgendaList?.SelectedItem is not AgendaItem item) return;
        var current = _agenda.IndexOf(item);
        var target = current + direction;
        if (target < 0 || target >= _agenda.Count) return;
        (_agenda[current], _agenda[target]) = (_agenda[target], _agenda[current]);
        Persist();
        RefreshAgenda();
        _activeAgendaList.SelectedIndex = target;
    }

    private void Persist()
    {
        try
        {
            _store.Save(_data);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, "Your changes could not be saved.\n\n" + exception.Message, "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
