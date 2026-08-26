namespace ChurchProjector;

public partial class Form1
{
    private async void SaveCurrentSong()
    {
        var title = _titleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show(this, "Enter a song title before saving.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _titleBox.Focus();
            return;
        }
        var lyrics = _lyricsBox.Text;
        var song = _currentSongId is Guid id ? _library.FirstOrDefault(item => item.Id == id) : null;
        var duplicate = _library.FirstOrDefault(s => s.Id != song?.Id && string.Equals(s.Title.Trim(), title, StringComparison.OrdinalIgnoreCase));
        if (duplicate is not null)
        {
            MessageBox.Show(this, $"Duplicates not allowed: A song with title \"{title}\" already exists.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var normLyrics = NormalizeLyrics(lyrics);
        if (!string.IsNullOrWhiteSpace(normLyrics))
        {
            var dupLyrics = _library.FirstOrDefault(s => s.Id != song?.Id && string.Equals(NormalizeLyrics(s.Lyrics), normLyrics, StringComparison.OrdinalIgnoreCase));
            if (dupLyrics is not null)
            {
                MessageBox.Show(this, $"Duplicates not allowed: Lyrics already exist in \"{dupLyrics.Title}\".", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        var isNew = song is null;
        var originalUpdatedAt = song?.UpdatedAt ?? default;
        if (song is null)
        {
            song = new Song { CreatedAt = DateTime.UtcNow };
            _library.Add(song);
            _currentSongId = song.Id;
        }
        song.Title = title;
        song.Lyrics = lyrics;
        song.Owner = "app";
        if (!song.Tags.Contains("church", StringComparer.OrdinalIgnoreCase)) song.Tags.Add("church");
        song.UpdatedAt = DateTime.UtcNow;
        if (isNew && song.CreatedAt == default) song.CreatedAt = DateTime.UtcNow;
        Persist();
        FilterLibrary("");
        if (_libraryList is not null) _libraryList.SelectedItem = song;
        _slideStatus.Text = "Saved - " + song.Title;

        if (_apiClient is not null)
        {
            try
            {
                var apiSong = ToApiSong(song);
                if (song.SourceId is null)
                {
                    apiSong.Tags = ["church"];
                    apiSong.Desktop = "true";
                    apiSong.Source = "desktop";
                    apiSong.Owner = "app";
                    var created = await _apiClient.CreateSongAsync(apiSong);
                    if (created is not null) song.SourceId = created.Id;
                    Persist();
                }
                else
                {
                    apiSong.Id = song.SourceId;
                    apiSong.ExpectedUpdatedAt = originalUpdatedAt == default ? null : originalUpdatedAt;
                    try
                    {
                        await _apiClient.UpdateSongAsync(song.SourceId, apiSong);
                    }
                    catch (ConflictException conflict)
                    {
                        var server = conflict.ServerSong;
                        var serverTitle = server?.Title ?? "(unknown)";
                        var result = MessageBox.Show(this,
                            $"Conflict detected: \"{title}\" was edited on the website more recently.\n\nServer (web) updated: {(server?.UpdatedAt.ToLocalTime().ToString() ?? "unknown")}\nYour version (app) updated: {song.UpdatedAt.ToLocalTime()}\n\nYes = Keep mine (overwrite web)\nNo = Take web version (discard my changes)\nCancel = Keep editing to merge manually",
                            "MPH Songs — Conflict",
                            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button3);
                        if (result == DialogResult.Yes)
                        {
                            apiSong.ExpectedUpdatedAt = null;
                            await _apiClient.UpdateSongAsync(song.SourceId, apiSong);
                            _slideStatus.Text = "Saved — overwrote web version";
                        }
                        else if (result == DialogResult.No)
                        {
                            if (server is not null)
                            {
                                song.Title = server.Title;
                                song.Lyrics = server.Lyrics;
                                song.Owner = server.Owner;
                                song.Tags = server.Tags;
                                song.UpdatedAt = server.UpdatedAt;
                                song.CreatedAt = server.CreatedAt;
                                Persist();
                                FilterLibrary(_librarySearch.Text);
                                LoadSong(song);
                                _slideStatus.Text = "Loaded web version — your changes discarded";
                            }
                        }
                        else
                        {
                            _slideStatus.Text = "Conflict — merge manually and save again";
                        }
                    }
                }
            }
            catch
            {
                // ignore sync failure; local save already succeeded
            }
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
        FilterLibrary("");
        NewSong();

        if (_apiClient is not null && song.SourceId is not null)
        {
            try { await _apiClient.DeleteSongAsync(song.SourceId); }
            catch { }
        }
    }

    private void NewSong()
    {
        LoadSongContent("", "", null, "New song");
        _titleBox.Focus();
    }



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

    private static string NormalizeLyrics(string lyrics) => lyrics.Replace("\r", "").Trim();

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
