namespace ChurchProjector;

public partial class Form1
{
    private void NewSong()
    {
        LoadSongContent("", "", null, "New song");
        _titleBox.Focus();
    }

    private void SaveCurrentSong()
    {
        var title = _titleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show(this, "Enter a song title before saving.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _titleBox.Focus();
            return;
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
        FilterLibrary("");
        _libraryList.SelectedItem = song;
        _slideStatus.Text = "Saved - " + song.Title;
    }

    private void DeleteCurrentSong()
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
        if (_agendaList.SelectedItem is not AgendaItem item) return;
        LoadSongContent(item.Title, item.LyricsSnapshot, item.SongId, "Loaded from Service Agenda");
    }

    private void RemoveAgendaItem()
    {
        if (_agendaList.SelectedItem is not AgendaItem item) return;
        var index = _agendaList.SelectedIndex;
        _agenda.Remove(item);
        Persist();
        RefreshAgenda();
        if (_agenda.Count > 0) _agendaList.SelectedIndex = Math.Min(index, _agenda.Count - 1);
    }

    private void UpdateAgendaItem()
    {
        if (_agendaList.SelectedItem is not AgendaItem item) return;
        item.Title = string.IsNullOrWhiteSpace(_titleBox.Text) ? "Untitled song" : _titleBox.Text.Trim();
        item.SongId = _currentSongId;
        item.LyricsSnapshot = _lyricsBox.Text;
        var index = _agendaList.SelectedIndex;
        Persist();
        RefreshAgenda();
        _agendaList.SelectedIndex = index;
    }

    private void MoveAgendaItem(int direction)
    {
        if (_agendaList.SelectedItem is not AgendaItem item) return;
        var current = _agenda.IndexOf(item);
        var target = current + direction;
        if (target < 0 || target >= _agenda.Count) return;
        (_agenda[current], _agenda[target]) = (_agenda[target], _agenda[current]);
        Persist();
        RefreshAgenda();
        _agendaList.SelectedIndex = target;
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
