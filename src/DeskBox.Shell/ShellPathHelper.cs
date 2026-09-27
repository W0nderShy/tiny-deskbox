namespace DeskBox.Shell;

internal static class ShellPathHelper
{
    private static readonly HashSet<string> DesktopShellDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "回收站",
        "Recycle Bin",
        "此电脑",
        "This PC",
        "控制面板",
        "Control Panel",
        "网络",
        "Network",
        "用户的文件",
        "User's Files"
    };

    public static bool IsShellNamespacePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        var trimmed = path.Trim();
        return trimmed.StartsWith("::", StringComparison.Ordinal)
               || trimmed.Contains("::{", StringComparison.OrdinalIgnoreCase);
    }

    public static bool PathExists(string path) =>
        File.Exists(path) || Directory.Exists(path);

    public static string? ValidateMovableSource(string path)
    {
        if (IsShellNamespacePath(path))
        {
            return "系统图标（如回收站、此电脑）无法移入盒子，仅支持文件、文件夹和快捷方式。";
        }

        if (!PathExists(path))
        {
            var name = Path.GetFileName(path.TrimEnd('\\'));
            if (DesktopShellDisplayNames.Contains(name))
            {
                return "系统图标（如回收站、此电脑、控制面板）无法移入盒子。";
            }

            return "无法访问该项目，请从资源管理器拖入文件或文件夹。";
        }

        return null;
    }

    public static string GetDisplayName(string path)
    {
        var name = Path.GetFileName(path.TrimEnd('\\', '/'));
        return string.IsNullOrEmpty(name) ? path : name;
    }

    public static bool IsSameVolume(string pathA, string pathB)
    {
        var rootA = Path.GetPathRoot(Path.GetFullPath(pathA));
        var rootB = Path.GetPathRoot(Path.GetFullPath(pathB));
        return !string.IsNullOrEmpty(rootA)
               && rootA.Equals(rootB, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSubPathOf(string parentDirectory, string childPath)
    {
        var parent = Path.GetFullPath(parentDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;
        var child = Path.GetFullPath(childPath);
        return child.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetUniqueDestinationPath(string destinationDirectory, string baseName, bool isDirectory)
    {
        var dir = Path.GetFullPath(destinationDirectory);
        var candidate = Path.Combine(dir, baseName);
        if (!File.Exists(candidate) && !Directory.Exists(candidate))
        {
            return candidate;
        }

        var stem = Path.GetFileNameWithoutExtension(baseName);
        var ext = Path.GetExtension(baseName);
        if (isDirectory)
        {
            stem = baseName;
            ext = string.Empty;
        }

        for (var i = 2; i < 1000; i++)
        {
            var altName = string.IsNullOrEmpty(ext) ? $"{stem} ({i})" : $"{stem} ({i}){ext}";
            candidate = Path.Combine(dir, altName);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("无法生成不冲突的目标名称。");
    }
}
