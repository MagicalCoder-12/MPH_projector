using System.IO.Compression;
using System.Text.Json;

namespace ChurchProjector;

public partial class Form1
{
    private void ExportLibrary()
    {
        using var dialog = new SaveFileDialog { Filter = "MPH library bundle|*.mphbundle|All files|*.*", Title = "Export library", FileName = "MPH_library.mphbundle" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var temp = Path.Combine(Path.GetTempPath(), "mph_export_" + Guid.NewGuid().ToString("N"));
        var media = Path.Combine(temp, "media");
        Directory.CreateDirectory(media);
        var clone = JsonSerializer.Deserialize<AppData>(_store.Serialize(_data))!;
        foreach (var asset in clone.Backgrounds)
        {
            if (string.IsNullOrWhiteSpace(asset.FilePath) || !File.Exists(asset.FilePath)) continue;
            var name = $"{asset.Id:N}{Path.GetExtension(asset.FilePath)}";
            File.Copy(asset.FilePath, Path.Combine(media, name), true);
            asset.FilePath = Path.Combine("media", name);
        }
        var logo = clone.BackgroundPreferences.LogoPath;
        if (!string.IsNullOrWhiteSpace(logo) && File.Exists(logo))
        {
            var name = "logo" + Path.GetExtension(logo);
            File.Copy(logo, Path.Combine(media, name), true);
            clone.BackgroundPreferences.LogoPath = Path.Combine("media", name);
        }
        File.WriteAllText(Path.Combine(temp, "library.json"), _store.Serialize(clone));
        if (File.Exists(dialog.FileName)) File.Delete(dialog.FileName);
        ZipFile.CreateFromDirectory(temp, dialog.FileName);
        Directory.Delete(temp, true);
        MessageBox.Show(this, "Library exported to:\n" + dialog.FileName, "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ImportLibrary()
    {
        using var dialog = new OpenFileDialog { Filter = "MPH library bundle|*.mphbundle|All files|*.*", Title = "Import library" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show(this, "Importing replaces the current songs, Bibles, agenda, and backgrounds on this computer. Continue?", "MPH Songs", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var temp = Path.Combine(Path.GetTempPath(), "mph_import_" + Guid.NewGuid().ToString("N"));
        try
        {
            ZipFile.ExtractToDirectory(dialog.FileName, temp);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, "This bundle could not be opened.\n\n" + exception.Message, "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var incoming = _store.Deserialize(File.ReadAllText(Path.Combine(temp, "library.json")));
        if (incoming is null)
        {
            MessageBox.Show(this, "This bundle does not contain a readable library.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        foreach (var asset in incoming.Backgrounds)
        {
            var source = Path.Combine(temp, asset.FilePath);
            if (!File.Exists(source)) continue;
            var imported = _store.ImportBackground(source, asset.Kind);
            asset.FilePath = imported.FilePath;
            asset.Id = imported.Id;
        }
        var logoSource = incoming.BackgroundPreferences.LogoPath;
        if (!string.IsNullOrWhiteSpace(logoSource) && File.Exists(Path.Combine(temp, logoSource)))
        {
            var imported = _store.ImportLogo(Path.Combine(temp, logoSource));
            if (!string.IsNullOrEmpty(imported)) incoming.BackgroundPreferences.LogoPath = imported;
        }
        _data.Songs = incoming.Songs;
        _data.Agenda = incoming.Agenda;
        _data.Bibles = incoming.Bibles;
        _data.Backgrounds = incoming.Backgrounds;
        _data.BackgroundPreferences = incoming.BackgroundPreferences;
        _library = _data.Songs;
        _agenda = _data.Agenda;
        _bibles = _data.Bibles;
        _backgrounds = _data.Backgrounds;
        Persist();
        LoadLogoImage();
        FilterLibrary("");
        RefreshBibleList();
        RefreshBibleTranslationPicker();
        RefreshBackgroundPicker();
        RefreshAgenda();
        if (_library.Count > 0) LoadSong(_library[0]);
        else NewSong();
        UpdateProjectorStatus();
        UpdateStageStatus();
        MessageBox.Show(this, "Library imported successfully.", "MPH Songs", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
