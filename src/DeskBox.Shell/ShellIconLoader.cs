using System.Drawing;
using System.Runtime.InteropServices;
using DeskBox.Shell.Native;

namespace DeskBox.Shell;

public sealed class ShellIconLoader : IDisposable
{
    private readonly Dictionary<string, (Icon Icon, DateTime Mtime)> _cache = new(StringComparer.OrdinalIgnoreCase);

    public Icon? GetIcon(string path, bool isDirectory)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        DateTime mtime = DateTime.MinValue;
        if (File.Exists(path))
        {
            mtime = File.GetLastWriteTimeUtc(path);
        }
        else if (Directory.Exists(path))
        {
            mtime = Directory.GetLastWriteTimeUtc(path);
        }

        var key = path + "|" + isDirectory;
        if (_cache.TryGetValue(key, out var cached) && cached.Mtime == mtime)
        {
            return (Icon)cached.Icon.Clone();
        }

        var attrs = isDirectory ? NativeMethods.FILE_ATTRIBUTE_DIRECTORY : 0u;
        var shfi = new NativeMethods.SHFILEINFO();
        var hIcon = NativeMethods.SHGetFileInfo(
            path,
            attrs,
            ref shfi,
            (uint)Marshal.SizeOf<NativeMethods.SHFILEINFO>(),
            NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON | NativeMethods.SHGFI_USEFILEATTRIBUTES);

        if (hIcon == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
        {
            return null;
        }

        var icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
        NativeMethods.DestroyIcon(shfi.hIcon);

        if (_cache.TryGetValue(key, out var old))
        {
            old.Icon.Dispose();
        }

        _cache[key] = (icon, mtime);
        return (Icon)icon.Clone();
    }

    public void Dispose()
    {
        foreach (var entry in _cache.Values)
        {
            entry.Icon.Dispose();
        }

        _cache.Clear();
    }
}
