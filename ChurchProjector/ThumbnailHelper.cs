using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ChurchProjector;

internal static class ThumbnailHelper
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Image> _memCache = new();
    private static readonly string _diskCacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MPH Songs", "thumbCache");
    private static readonly object _diskLock = new();

    public static Image? Create(BackgroundAsset asset, int width, int height)
    {
        var cacheKey = $"{asset.FilePath}|{width}x{height}";
        try
        {
            if (!File.Exists(asset.FilePath)) return Placeholder(asset.Kind == "Video", width, height);
            var writeTime = File.GetLastWriteTimeUtc(asset.FilePath);
            var diskKey = GetDiskCachePath(asset.FilePath, width, height, writeTime);
            lock (_diskLock) Directory.CreateDirectory(_diskCacheDir);
            if (File.Exists(diskKey))
            {
                try
                {
                    using var cached = Image.FromFile(diskKey);
                    var clone = new Bitmap(cached);
                    _memCache[cacheKey] = new Bitmap(clone);
                    return clone;
                }
                catch { try { File.Delete(diskKey); } catch { } }
            }
            if (_memCache.TryGetValue(cacheKey, out var mem) && mem is not null)
            {
                try { return new Bitmap(mem); } catch { _memCache.TryRemove(cacheKey, out _); }
            }
            Image? result;
            if (asset.Kind == "Video")
            {
                var shell = ShellThumbnail(asset.FilePath, width, height, true) ?? ShellThumbnail(asset.FilePath, width, height, false);
                if (shell is null) result = Placeholder(true, width, height);
                else using (shell) result = Fit(shell, width, height);
            }
            else
            {
                using var src = Image.FromFile(asset.FilePath);
                result = Fit(src, width, height);
            }
            if (result is not null)
            {
                try
                {
                    _memCache[cacheKey] = new Bitmap(result);
                    lock (_diskLock) { try { result.Save(diskKey, System.Drawing.Imaging.ImageFormat.Jpeg); } catch { } }
                }
                catch { }
            }
            return result;
        }
        catch { return Placeholder(asset.Kind == "Video", width, height); }
    }

    private static string GetDiskCachePath(string filePath, int w, int h, DateTime writeTime)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(filePath.ToLowerInvariant() + writeTime.Ticks.ToString())));
        return Path.Combine(_diskCacheDir, $"{hash[..16]}_{w}x{h}.jpg");
    }

    public static void InvalidateCache(string filePath)
    {
        try
        {
            var prefix = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(filePath.ToLowerInvariant())))[..16];
            if (Directory.Exists(_diskCacheDir))
            {
                foreach (var f in Directory.EnumerateFiles(_diskCacheDir, $"{prefix}_*.jpg"))
                    try { File.Delete(f); } catch { }
            }
            var keys = _memCache.Keys.Where(k => k.StartsWith(filePath + "|", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var k in keys) if (_memCache.TryRemove(k, out var img)) try { img.Dispose(); } catch { }
        }
        catch { }
    }

    public static Bitmap Fit(Image src, int w, int h)
    {
        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var scale = Math.Min((double)w / Math.Max(1, src.Width), (double)h / Math.Max(1, src.Height));
        var nw = Math.Max(1, (int)Math.Round(src.Width * scale));
        var nh = Math.Max(1, (int)Math.Round(src.Height * scale));
        g.Clear(Color.Transparent);
        g.DrawImage(src, (w - nw) / 2, (h - nh) / 2, nw, nh);
        return bmp;
    }

    public static Bitmap Placeholder(bool video, int w, int h)
    {
        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        g.Clear(video ? Color.FromArgb(40, 46, 56) : Color.FromArgb(230, 234, 238));
        if (video)
        {
            using var brush = new SolidBrush(Color.White);
            var cx = w / 2f; var cy = h / 2f;
            g.FillPolygon(brush, [new PointF(cx - 6, cy - 8), new PointF(cx - 6, cy + 8), new PointF(cx + 10, cy)]);
        }
        return bmp;
    }

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig] int GetImage(NativeSize size, int flags, out IntPtr bitmap);
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeSize { public int Width; public int Height; }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(string path, IntPtr pbc, in Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory factory);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    public static Image? ShellThumbnail(string path, int w, int h, bool thumb)
    {
        try
        {
            SHCreateItemFromParsingName(path, IntPtr.Zero, new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), out var f);
            var r = f.GetImage(new NativeSize { Width = w, Height = h }, thumb ? 8 : 4, out var bmp);
            if (r != 0 || bmp == IntPtr.Zero) return null;
            try { using var tmp = Image.FromHbitmap(bmp); return new Bitmap(tmp); } finally { DeleteObject(bmp); }
        }
        catch { return null; }
    }
}
