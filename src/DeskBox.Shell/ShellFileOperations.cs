using DeskBox.Core.Services;

namespace DeskBox.Shell;

public sealed class ShellFileOperations
{
    private readonly ShellDesktopService _desktop;

    public ShellFileOperations(ShellDesktopService desktop)
    {
        _desktop = desktop;
    }

    public OperationResult MoveItems(IEnumerable<string> sourcePaths, string destinationDirectory)
    {
        try
        {
            Directory.CreateDirectory(destinationDirectory);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"无法创建目标文件夹：{ex.Message}");
        }

        var sources = sourcePaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sources.Count == 0)
        {
            return OperationResult.Fail("没有可移动的文件。");
        }

        var errors = new List<string>();
        var movedAny = false;

        foreach (var source in sources)
        {
            var validation = ShellPathHelper.ValidateMovableSource(source);
            if (validation is not null)
            {
                errors.Add(validation);
                continue;
            }

            try
            {
                MoveSingleItem(source, destinationDirectory);
                movedAny = true;
            }
            catch (Exception ex)
            {
                errors.Add($"{ShellPathHelper.GetDisplayName(source)}：{DescribeMoveError(ex)}");
            }
        }

        if (!movedAny)
        {
            return OperationResult.Fail(errors.Count == 1 ? errors[0] : string.Join(Environment.NewLine, errors));
        }

        if (errors.Count > 0)
        {
            _desktop.RefreshDesktop();
            return OperationResult.Fail(
                $"已移动部分项目，但有 {errors.Count} 项失败：{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        _desktop.RefreshDesktop();
        return OperationResult.Ok();
    }

    public OperationResult MoveToBox(IEnumerable<string> sourcePaths, Guid boxId) =>
        MoveItems(sourcePaths, AppPaths.BoxStorage(boxId));

    public OperationResult MoveToDesktop(IEnumerable<string> sourcePaths) =>
        MoveItems(sourcePaths, _desktop.GetUserDesktopPath());

    public OperationResult MoveAllBoxItemsToDesktop(Guid boxId)
    {
        var dir = AppPaths.BoxStorage(boxId);
        if (!Directory.Exists(dir))
        {
            return OperationResult.Ok();
        }

        var items = Directory.EnumerateFileSystemEntries(dir).ToList();
        if (items.Count == 0)
        {
            return OperationResult.Ok();
        }

        return MoveToDesktop(items);
    }

    private static void MoveSingleItem(string source, string destinationDirectory)
    {
        var fullSource = Path.GetFullPath(source);
        var destDir = Path.GetFullPath(destinationDirectory);

        if (ShellPathHelper.IsSubPathOf(fullSource, destDir))
        {
            throw new InvalidOperationException("不能将文件夹移动到它自身的内部。");
        }

        var name = Path.GetFileName(fullSource.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(name))
        {
            throw new InvalidOperationException("无法识别要移动的项目名称。");
        }

        var destPath = Path.Combine(destDir, name);
        if (string.Equals(fullSource, Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Directory.Exists(fullSource))
        {
            if (Directory.Exists(destPath))
            {
                MergeDirectoryContents(fullSource, destPath);
                Directory.Delete(fullSource, recursive: true);
                return;
            }

            if (File.Exists(destPath))
            {
                throw new IOException("已存在同名文件，无法放入文件夹。");
            }

            MoveDirectory(fullSource, destPath);
            return;
        }

        if (File.Exists(fullSource))
        {
            if (Directory.Exists(destPath))
            {
                throw new IOException("已存在同名文件夹，无法放入文件。");
            }

            if (File.Exists(destPath))
            {
                destPath = ShellPathHelper.GetUniqueDestinationPath(destDir, name, isDirectory: false);
            }

            MoveFile(fullSource, destPath);
            return;
        }

        throw new FileNotFoundException("找不到源文件或文件夹。", fullSource);
    }

    private static void MoveFile(string source, string destination)
    {
        if (ShellPathHelper.IsSameVolume(source, destination))
        {
            File.Move(source, destination);
            return;
        }

        File.Copy(source, destination, overwrite: false);
        File.Delete(source);
    }

    private static void MoveDirectory(string source, string destination)
    {
        if (ShellPathHelper.IsSameVolume(source, destination))
        {
            try
            {
                Directory.Move(source, destination);
                return;
            }
            catch (IOException)
            {
                // 跨卷或占用时回退到复制后删除
            }
        }

        CopyDirectory(source, destination);
        Directory.Delete(source, recursive: true);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, target, overwrite: false);
        }

        foreach (var dir in Directory.EnumerateDirectories(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(dir));
            CopyDirectory(dir, target);
        }
    }

    private static void MergeDirectoryContents(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(file));
            if (File.Exists(target))
            {
                continue;
            }

            MoveFile(file, target);
        }

        foreach (var dir in Directory.EnumerateDirectories(source))
        {
            var dirName = Path.GetFileName(dir);
            var target = Path.Combine(destination, dirName);
            if (Directory.Exists(target))
            {
                MergeDirectoryContents(dir, target);
                Directory.Delete(dir, recursive: true);
            }
            else
            {
                MoveDirectory(dir, target);
            }
        }
    }

    private static string DescribeMoveError(Exception ex)
    {
        if (ex is IOException
            && (ex.HResult == unchecked((int)0x80070020)
                || ex.Message.Contains("being used", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("正在使用", StringComparison.OrdinalIgnoreCase)))
        {
            return "文件或文件夹正在被其他程序使用，请关闭相关窗口后重试。";
        }

        if (ex is UnauthorizedAccessException)
        {
            return "没有权限移动该项目。";
        }

        return ex.Message;
    }

}

public readonly struct OperationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static OperationResult Ok() => new() { Success = true };
    public static OperationResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
