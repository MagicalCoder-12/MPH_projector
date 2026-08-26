using System.Drawing.Drawing2D;

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
        _theme.AutoFit = preferences.AutoFit;
        if (preferences.SelectedBackgroundId is not Guid id) return;
        var asset = _backgrounds.FirstOrDefault(item => item.Id == id);
        if (asset is { } savedAsset && File.Exists(savedAsset.FilePath)) ApplyBackgroundAsset(savedAsset, false);
    }

    private void RefreshBackgroundPicker()
    {
        if (_backgroundList is null) return;
        _updating = true;
        _backgroundList.BeginUpdate();
        _backgroundList.Items.Clear();

        foreach (var thumb in _backgroundThumbs) thumb.Dispose();
        _backgroundThumbs.Clear();
        _backgroundList.LargeImageList?.Dispose();

        var images = new ImageList { ImageSize = new Size(72, 40), ColorDepth = ColorDepth.Depth32Bit };
        _backgroundList.LargeImageList = images;
        var selectedId = _theme.BackgroundAssetId;
        foreach (var asset in _backgrounds.OrderBy(item => item.Name))
        {
            var thumb = MakeBackgroundThumbnail(asset);
            _backgroundThumbs.Add(thumb);
            images.Images.Add(thumb);
            var item = new ListViewItem(asset.Name) { Tag = asset, ImageIndex = images.Images.Count - 1 };
            _backgroundList.Items.Add(item);
            if (asset.Id == selectedId) item.Selected = true;
        }
        _backgroundList.EndUpdate();
        _updating = false;
    }

    private Bitmap MakeBackgroundThumbnail(BackgroundAsset asset)
    {
        var thumb = new Bitmap(72, 40);
        try
        {
            using var graphics = Graphics.FromImage(thumb);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            if (asset.Kind == "Video" || !File.Exists(asset.FilePath))
            {
                graphics.Clear(Color.FromArgb(31, 46, 66));
                using var font = new Font("Segoe UI", 8F);
                using var brush = new SolidBrush(Color.FromArgb(190, 220, 240));
                using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                graphics.DrawString(asset.Kind == "Video" ? "VIDEO" : "MISSING", font, brush, new RectangleF(0, 0, 72, 40), format);
            }
            else
            {
                using var source = Image.FromFile(asset.FilePath);
                var scale = Math.Max(72f / source.Width, 40f / source.Height);
                var width = source.Width * scale;
                var height = source.Height * scale;
                var x = (72f - width) / 2f;
                var y = (40f - height) / 2f;
                graphics.DrawImage(source, new RectangleF(x, y, width, height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
                using var shade = new SolidBrush(Color.FromArgb(70, 0, 0, 0));
                graphics.FillRectangle(shade, 0, 0, 72, 40);
            }
            return thumb;
        }
        catch
        {
            using var graphics = Graphics.FromImage(thumb);
            graphics.Clear(Color.FromArgb(88, 100, 114));
            return thumb;
        }
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

    private void DeleteBackgroundAsset()
    {
        if (_backgroundList is null || _backgroundList.SelectedItems.Count == 0) return;
        var asset = (BackgroundAsset)_backgroundList.SelectedItems[0].Tag!;
        if (!DialogHelpers.Confirm(this, "Remove background",
                $"Remove '{asset.Name}' from the background library?\n\nThe file is deleted from this computer.", destructive: true)) return;
        if (!_store.DeleteBackgroundFile(asset.FilePath))
        {
            MessageBox.Show(this, "The file could not be deleted because it is not part of the MPH background library.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _backgrounds.Remove(asset);
        Persist();
        if (_theme.BackgroundAssetId == asset.Id) ClearBackgroundSelection();
        RefreshBackgroundPicker();
        RefreshSlides();
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
            if (_backgroundList is not null)
            {
                _updating = true;
                foreach (ListViewItem item in _backgroundList.Items)
                {
                    if ((item.Tag as BackgroundAsset)?.Id == asset.Id)
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                    }
                }
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
        if (_backgroundList is not null)
        {
            _updating = true;
            foreach (ListViewItem item in _backgroundList.Items) item.Selected = false;
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
        _data.BackgroundPreferences.AutoFit = _theme.AutoFit;
        Persist();
    }
}
