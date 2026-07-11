namespace ChurchProjector;

public partial class Form1
{
    private void SyncConfiguredBackgrounds()
    {
        var changed = false;
        foreach (var discovered in _store.DiscoverBackgroundAssets())
        {
            if (_backgrounds.Any(item => string.Equals(item.FilePath, discovered.FilePath, StringComparison.OrdinalIgnoreCase))) continue;
            _backgrounds.Add(discovered);
            changed = true;
        }
        if (changed) Persist();
    }

    private void RestoreBackgroundPreferences()
    {
        var preferences = _data.BackgroundPreferences ?? new BackgroundPreferences();
        _data.BackgroundPreferences = preferences;
        _theme.BackgroundColor = Color.FromArgb(preferences.BackgroundColorArgb);
        _theme.Brightness = Math.Clamp(preferences.Brightness, -75, 75);
        _theme.AspectRatio = string.IsNullOrWhiteSpace(preferences.AspectRatio) ? "16:9" : preferences.AspectRatio;
        _theme.VideoLoop = preferences.VideoLoop;
        if (preferences.SelectedBackgroundId is not Guid id) return;
        var asset = _backgrounds.FirstOrDefault(item => item.Id == id);
        if (asset is { } savedAsset && File.Exists(savedAsset.FilePath)) ApplyBackgroundAsset(savedAsset, false);
    }

    private void RefreshBackgroundPicker()
    {
        if (_backgroundPicker is null) return;
        _updating = true;
        _backgroundPicker.BeginUpdate();
        _backgroundPicker.Items.Clear();
        foreach (var asset in _backgrounds.OrderBy(item => item.Name)) _backgroundPicker.Items.Add(asset);
        _backgroundPicker.SelectedItem = _backgrounds.FirstOrDefault(item => item.Id == _theme.BackgroundAssetId);
        _backgroundPicker.EndUpdate();
        _updating = false;
    }

    private void ImportBackground(string kind)
    {
        var filter = kind == "Video"
            ? "Video files|*.mp4;*.wmv;*.avi;*.mov;*.m4v|All files|*.*"
            : "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*";
        using var dialog = new OpenFileDialog { Filter = filter, Title = $"Import {kind.ToLowerInvariant()} background" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var asset = _store.ImportBackground(dialog.FileName, kind);
            _backgrounds.Add(asset);
            Persist();
            RefreshBackgroundPicker();
            ApplyBackgroundAsset(asset);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"The {kind.ToLowerInvariant()} could not be added to the background library.\n\n" + exception.Message, "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ApplyBackgroundAsset(BackgroundAsset asset, bool save = true)
    {
        if (!File.Exists(asset.FilePath))
        {
            MessageBox.Show(this, "This saved background file is no longer available.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (_backgroundPicker is not null && _backgroundPicker.SelectedItem != asset)
            {
                _updating = true;
                _backgroundPicker.SelectedItem = asset;
                _updating = false;
            }
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
        if (_backgroundPicker is not null)
        {
            _updating = true;
            _backgroundPicker.SelectedIndex = -1;
            _updating = false;
        }
    }

    private void SaveBackgroundPreferences()
    {
        _data.BackgroundPreferences.BackgroundColorArgb = _theme.BackgroundColor.ToArgb();
        _data.BackgroundPreferences.Brightness = _theme.Brightness;
        _data.BackgroundPreferences.AspectRatio = _theme.AspectRatio;
        _data.BackgroundPreferences.SelectedBackgroundId = _theme.BackgroundAssetId;
        _data.BackgroundPreferences.VideoLoop = _theme.VideoLoop;
        Persist();
    }
}
