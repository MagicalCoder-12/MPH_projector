using System.Diagnostics;

namespace ChurchProjector;

public partial class Form1
{
    private void SyncConfiguredBackgrounds()
    {
        var changed = false;
        foreach (var discovered in _store.DiscoverBackgroundAssets())
        {
            if (_backgrounds.Any(item => string.Equals(item.FilePath, discovered.FilePath, StringComparison.OrdinalIgnoreCase))) continue;
            if (_backgrounds.Any(item => item.Kind == discovered.Kind && string.Equals(item.Name, discovered.Name, StringComparison.OrdinalIgnoreCase))) continue;
            _backgrounds.Add(discovered);
            changed = true;
        }
        if (changed) Persist();
    }

    private void SetupBackgroundWatchers()
    {
        try
        {
            _bgWatcherDebounce.Tick += (_, _) =>
            {
                _bgWatcherDebounce.Stop();
                if (!_bgWatcherPending) return;
                _bgWatcherPending = false;
                if (IsDisposed || Disposing) return;
                BeginInvoke(new Action(SyncBackgroundsFromDisk));
            };
            _bgPollTimer.Tick += (_, _) =>
            {
                if (IsDisposed || Disposing) return;
                if (_backgrounds.Any(b => !File.Exists(b.FilePath))) SyncBackgroundsFromDisk();
            };
            _bgPollTimer.Start();
            var imgFolder = _store.GetBackgroundFolder("Image");
            var vidFolder = _store.GetBackgroundFolder("Video");
            Directory.CreateDirectory(imgFolder);
            Directory.CreateDirectory(vidFolder);
            _imageWatcher = new FileSystemWatcher(imgFolder) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.CreationTime, EnableRaisingEvents = true, IncludeSubdirectories = false };
            _imageWatcher.Created += (_, _) => QueueBgWatcher();
            _imageWatcher.Deleted += (_, _) => QueueBgWatcher();
            _imageWatcher.Renamed += (_, _) => QueueBgWatcher();
            _imageWatcher.Changed += (_, _) => QueueBgWatcher();
            _videoWatcher = new FileSystemWatcher(vidFolder) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.CreationTime, EnableRaisingEvents = true, IncludeSubdirectories = false };
            _videoWatcher.Created += (_, _) => QueueBgWatcher();
            _videoWatcher.Deleted += (_, _) => QueueBgWatcher();
            _videoWatcher.Renamed += (_, _) => QueueBgWatcher();
            _videoWatcher.Changed += (_, _) => QueueBgWatcher();
        }
        catch { }
    }

    private void QueueBgWatcher()
    {
        _bgWatcherPending = true;
        if (InvokeRequired) BeginInvoke(new Action(() => { if (!_bgWatcherDebounce.Enabled) _bgWatcherDebounce.Start(); }));
        else if (!_bgWatcherDebounce.Enabled) _bgWatcherDebounce.Start();
    }

    private void SyncBackgroundsFromDisk()
    {
        var missing = _backgrounds.Where(b => !File.Exists(b.FilePath)).ToList();
        var changed = false;
        foreach (var r in missing)
        {
            _backgrounds.Remove(r);
            ThumbnailHelper.InvalidateCache(r.FilePath);
            if (_theme.BackgroundAssetId == r.Id)
            {
                ClearBackgroundSelection();
                SaveBackgroundPreferences();
            }
            changed = true;
        }
        var discovered = _store.DiscoverBackgroundAssets().ToList();
        foreach (var d in discovered)
        {
            if (_backgrounds.Any(b => string.Equals(b.FilePath, d.FilePath, StringComparison.OrdinalIgnoreCase))) continue;
            if (_backgrounds.Any(b => b.Kind == d.Kind && string.Equals(b.Name, d.Name, StringComparison.OrdinalIgnoreCase))) continue;
            _backgrounds.Add(d);
            changed = true;
        }
        if (changed)
        {
            Persist();
            RefreshBackgroundGalleries();
            _activeDropdown?.SetAssets(_activeDropdown.Kind == "Video" ? _backgrounds.Where(a => a.Kind == "Video") : _backgrounds.Where(a => a.Kind != "Video"), _theme.BackgroundAssetId);
            RefreshSlides();
        }
        else
        {
            var anyMissingInUi = false;
            if (_imageGallery is not null || _videoGallery is not null)
            {
                var hasGhost = _backgrounds.Any(b => !File.Exists(b.FilePath));
                if (hasGhost) { RefreshBackgroundGalleries(); anyMissingInUi = true; }
            }
            if (anyMissingInUi) Persist();
        }
    }

    private void RestoreBackgroundPreferences()
    {
        var preferences = _data.BackgroundPreferences ?? new BackgroundPreferences();
        _data.BackgroundPreferences = preferences;
        _theme.BackgroundColor = Color.FromArgb(preferences.BackgroundColorArgb);
        _theme.Brightness = Math.Clamp(preferences.Brightness, -75, 75);
        _theme.AspectRatio = string.IsNullOrWhiteSpace(preferences.AspectRatio) ? "16:9" : preferences.AspectRatio;
        _theme.VideoLoop = preferences.VideoLoop;
        _theme.AutoFit = preferences.AutoFit;
        if (preferences.SelectedBackgroundId is not Guid id) return;
        var asset = _backgrounds.FirstOrDefault(item => item.Id == id);
        if (asset is { } savedAsset && File.Exists(savedAsset.FilePath)) ApplyBackgroundAsset(savedAsset, false);
    }

    private void RefreshBackgroundGalleries()
    {
        if (_imageGallery is null || _videoGallery is null) return;
        var ghosts = _backgrounds.Where(b => !File.Exists(b.FilePath)).ToList();
        if (ghosts.Count > 0)
        {
            foreach (var g in ghosts) { _backgrounds.Remove(g); ThumbnailHelper.InvalidateCache(g.FilePath); if (_theme.BackgroundAssetId == g.Id) { ClearBackgroundSelection(); SaveBackgroundPreferences(); } }
            Persist();
        }
        _imageGallery.SetAssets(_backgrounds.Where(item => item.Kind != "Video").OrderBy(item => item.Name));
        _videoGallery.SetAssets(_backgrounds.Where(item => item.Kind == "Video").OrderBy(item => item.Name));
        SyncBackgroundGallerySelection();
    }

    private void SyncBackgroundGallerySelection()
    {
        if (_imageGallery is null || _videoGallery is null) return;
        _imageGallery.SelectAsset(_theme.BackgroundAssetId);
        _videoGallery.SelectAsset(_theme.BackgroundAssetId);
    }

    private async void ImportBackground(string kind)
    {
        var filter = kind == "Video"
            ? "Video files|*.mp4;*.wmv;*.avi;*.mov;*.m4v|All files|*.*"
            : "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*";
        using var dialog = new OpenFileDialog { Filter = filter, Title = $"Import {kind.ToLowerInvariant()} backgrounds", Multiselect = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var failures = new List<string>();
        var existingNames = new HashSet<string>(_backgrounds.Where(a => a.Kind == kind).Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
        var batchNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateFiles = new List<string>();
        BackgroundAsset? last = null;
        Enabled = false;
        UseWaitCursor = true;
        try
        {
            foreach (var file in dialog.FileNames)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (existingNames.Contains(name) || !batchNames.Add(name))
                {
                    duplicateFiles.Add(Path.GetFileName(file));
                    continue;
                }
                try
                {
                    var asset = await Task.Run(() => _store.ImportBackground(file, kind));
                    _backgrounds.Add(asset);
                    last = asset;
                    existingNames.Add(name);
                }
                catch (Exception exception)
                {
                    batchNames.Remove(name);
                    failures.Add($"{Path.GetFileName(file)} — {exception.Message}");
                }
            }
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
        var duplicateCount = duplicateFiles.Count;
        if (duplicateCount > 0 && last is null && failures.Count == 0)
        {
            MessageBox.Show(this, $"Duplicates not allowed: \"{string.Join("\", \"", duplicateFiles)}\" already exists in the {kind.ToLowerInvariant()} library.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (last is not null)
        {
            Persist();
            RefreshBackgroundGalleries();
            ApplyBackgroundAsset(last);
        }
        var messages = new List<string>();
        if (duplicateCount > 0)
            messages.Add($"{duplicateCount} duplicate(s) were skipped — duplicates not allowed{(duplicateFiles.Count <= 5 ? $": {string.Join(", ", duplicateFiles)}" : "")}.");
        if (failures.Count > 0)
            messages.Add("Some files could not be added:\n" + string.Join("\n", failures));
        if (messages.Count > 0 && (last is not null || failures.Count > 0))
            MessageBox.Show(this, string.Join("\n\n", messages), "MPH Songs", MessageBoxButtons.OK, duplicateCount > 0 && failures.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        else if (messages.Count > 0)
            MessageBox.Show(this, string.Join("\n\n", messages), "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ApplyBackgroundAsset(BackgroundAsset asset, bool save = true)
    {
        if (!File.Exists(asset.FilePath))
        {
            var ghost = _backgrounds.FirstOrDefault(b => b.Id == asset.Id);
            if (ghost is not null)
            {
                _backgrounds.Remove(ghost);
                ThumbnailHelper.InvalidateCache(ghost.FilePath);
                Persist();
                RefreshBackgroundGalleries();
                _activeDropdown?.SetAssets(_activeDropdown.Kind == "Video" ? _backgrounds.Where(a => a.Kind == "Video") : _backgrounds.Where(a => a.Kind != "Video"), _theme.BackgroundAssetId);
            }
            MessageBox.Show(this, "This saved background file is no longer available. The gallery has been refreshed.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            _theme.BackgroundImage?.Dispose();
            _theme.BackgroundImage = null;
            _theme.BackgroundVideoPath = null;
            _theme.BackgroundAssetId = asset.Id;
            if (asset.Kind == "Video")
            {
                _theme.BackgroundVideoPath = asset.FilePath;
            }
            else
            {
                using var source = Image.FromFile(asset.FilePath);
                _theme.BackgroundImage = new Bitmap(source);
            }
            SyncBackgroundGallerySelection();
            if (save) SaveBackgroundPreferences();
            RefreshSlides();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, "The selected background could not be used.\n\n" + exception.Message, "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SetSolidBackground(Color color)
    {
        _theme.BackgroundColor = color;
        ClearBackgroundSelection();
        SaveBackgroundPreferences();
        RefreshSlides();
    }

    private void ClearBackgroundSelection()
    {
        _theme.BackgroundImage?.Dispose();
        _theme.BackgroundImage = null;
        _theme.BackgroundVideoPath = null;
        _theme.BackgroundAssetId = null;
        _imageGallery?.SelectAsset(null);
        _videoGallery?.SelectAsset(null);
    }

    private void SaveBackgroundPreferences()
    {
        _data.BackgroundPreferences.BackgroundColorArgb = _theme.BackgroundColor.ToArgb();
        _data.BackgroundPreferences.Brightness = _theme.Brightness;
        _data.BackgroundPreferences.AspectRatio = _theme.AspectRatio;
        _data.BackgroundPreferences.SelectedBackgroundId = _theme.BackgroundAssetId;
        _data.BackgroundPreferences.VideoLoop = _theme.VideoLoop;
        _data.BackgroundPreferences.AutoFit = _theme.AutoFit;
        Persist();
    }

    private void DeleteBackgroundAsset(BackgroundAsset asset)
    {
        if (MessageBox.Show(this, $"Delete \"{asset.Name}\" from the library?\nThe file will be removed from the MPH Songs folder.", "MPH Songs", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _backgrounds.RemoveAll(a => a.Id == asset.Id);
        try { if (File.Exists(asset.FilePath)) File.Delete(asset.FilePath); } catch { }
        if (_theme.BackgroundAssetId == asset.Id)
        {
            ClearBackgroundSelection();
            SaveBackgroundPreferences();
            RefreshSlides();
        }
        Persist();
        RefreshBackgroundGalleries();
    }

    private void ExploreBackgroundFolder(string kind)
    {
        var folder = _store.GetBackgroundFolder(kind);
        try
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not open folder:\n" + ex.Message, "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowGalleryDropdown(string kind, Control anchor)
    {
        SyncBackgroundsFromDisk();
        if (_activeDropdown is not null && !_activeDropdown.IsDisposed)
        {
            var wasSame = _activeDropdown.Kind == kind && _activeDropdown.Visible;
            _activeDropdown.Close();
            _activeDropdown = null;
            if (wasSame) return;
        }
        var assets = kind == "Video"
            ? _backgrounds.Where(a => a.Kind == "Video").OrderBy(a => a.Name)
            : _backgrounds.Where(a => a.Kind != "Video").OrderBy(a => a.Name);
        var dropdown = new GalleryDropdownForm(kind, assets, _theme.BackgroundAssetId);
        dropdown.AssetSelected += asset =>
        {
            if (asset is null) SetSolidBackground(Color.Black);
            else ApplyBackgroundAsset(asset);
        };
        dropdown.ImportRequested += () =>
        {
            ImportBackground(kind);
            var refreshed = kind == "Video" ? _backgrounds.Where(a => a.Kind == "Video") : _backgrounds.Where(a => a.Kind != "Video");
            dropdown.SetAssets(refreshed, _theme.BackgroundAssetId);
            RefreshBackgroundGalleries();
        };
        dropdown.DeleteRequested += asset =>
        {
            DeleteBackgroundAsset(asset);
            var refreshed = kind == "Video" ? _backgrounds.Where(a => a.Kind == "Video") : _backgrounds.Where(a => a.Kind != "Video");
            dropdown.SetAssets(refreshed, _theme.BackgroundAssetId);
        };
        dropdown.ExploreRequested += () => ExploreBackgroundFolder(kind);
        _activeDropdown = dropdown;
        dropdown.FormClosed += (_, _) => { if (_activeDropdown == dropdown) _activeDropdown = null; RefreshBackgroundGalleries(); };
        var screenPos = anchor.PointToScreen(new Point(0, anchor.Height));
        dropdown.StartPosition = FormStartPosition.Manual;
        dropdown.Location = screenPos;
        var screen = Screen.FromControl(this);
        if (dropdown.Right > screen.WorkingArea.Right) dropdown.Left = screen.WorkingArea.Right - dropdown.Width - 6;
        if (dropdown.Bottom > screen.WorkingArea.Bottom) dropdown.Top = anchor.PointToScreen(Point.Empty).Y - dropdown.Height - 2;
        if (dropdown.Top < screen.WorkingArea.Top) dropdown.Top = screen.WorkingArea.Top + 2;
        dropdown.Show(this);
    }
}
