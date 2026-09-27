using DeskBox.Core.Models;

namespace DeskBox.Core.Services;

public sealed class BoxRepository
{
    private readonly LayoutStore _layoutStore;
    private LayoutDocument? _layoutCache;

    public BoxRepository(LayoutStore layoutStore)
    {
        _layoutStore = layoutStore;
    }

    public LayoutDocument LoadLayout()
    {
        _layoutCache ??= RecoverLayoutIfNeeded(_layoutStore.Load());
        return _layoutCache;
    }

    public void ReloadLayout() => _layoutCache = RecoverLayoutIfNeeded(_layoutStore.Load());

    public void SaveLayout(LayoutDocument layout)
    {
        _layoutCache = layout;
        _layoutStore.Save(layout);
    }

    public BoxModel? FindBox(Guid id) =>
        LoadLayout().Boxes.FirstOrDefault(b => b.Id == id);

    public void SyncModelFromCache(BoxModel model)
    {
        var existing = FindBox(model.Id);
        if (existing is null)
        {
            return;
        }

        CopyLayout(existing, model);
    }

    public BoxModel CreateBox(double x, double y)
    {
        var layout = LoadLayout();
        var box = new BoxModel
        {
            Title = $"盒子 {layout.Boxes.Count + 1}",
            X = x,
            Y = y,
            SortOrder = layout.Boxes.Count
        };
        box.ExpandedHeight = box.Height;
        layout.Boxes.Add(box);
        SaveLayout(layout);
        Directory.CreateDirectory(AppPaths.BoxStorage(box.Id));
        return box;
    }

    public void UpdateBox(BoxModel box)
    {
        BoxLayoutNormalizer.Normalize(box);

        var layout = LoadLayout();
        var existing = layout.Boxes.FirstOrDefault(b => b.Id == box.Id);
        if (existing is null)
        {
            return;
        }

        if (!ReferenceEquals(existing, box))
        {
            CopyLayout(box, existing);
        }

        SaveLayout(layout);
    }

    public void DeleteBox(Guid id, bool restoreItemsToDesktop)
    {
        var layout = LoadLayout();
        layout.Boxes.RemoveAll(b => b.Id == id);
        SaveLayout(layout);

        var storage = AppPaths.BoxStorage(id);
        if (!Directory.Exists(storage))
        {
            return;
        }

        if (restoreItemsToDesktop)
        {
            // Caller moves files via ShellFileOperations before delete.
        }

        try
        {
            Directory.Delete(storage, recursive: true);
        }
        catch
        {
            // Best effort.
        }
    }

    public IReadOnlyList<BoxItemInfo> ListItems(Guid boxId)
    {
        var dir = AppPaths.BoxStorage(boxId);
        if (!Directory.Exists(dir))
        {
            return Array.Empty<BoxItemInfo>();
        }

        return Directory.EnumerateFileSystemEntries(dir)
            .Select(path =>
            {
                var name = Path.GetFileName(path);
                var attrs = File.GetAttributes(path);
                return new BoxItemInfo
                {
                    FullPath = path,
                    DisplayName = name,
                    IsDirectory = attrs.HasFlag(FileAttributes.Directory)
                };
            })
            .OrderBy(i => i.IsDirectory)
            .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void CopyLayout(BoxModel from, BoxModel to)
    {
        to.Title = from.Title;
        to.X = from.X;
        to.Y = from.Y;
        to.Width = from.Width;
        to.Height = from.Height;
        to.ExpandedHeight = from.ExpandedHeight;
        to.Collapsed = from.Collapsed;
        to.AutoCollapsePaused = from.AutoCollapsePaused;
        to.SortOrder = from.SortOrder;
    }

    private LayoutDocument RecoverLayoutIfNeeded(LayoutDocument layout)
    {
        if (layout.Boxes.Count > 0 || !Directory.Exists(AppPaths.StorageRoot))
        {
            return layout;
        }

        var storageDirs = Directory.EnumerateDirectories(AppPaths.StorageRoot)
            .Select(path => new { Path = path, Name = Path.GetFileName(path) })
            .Where(dir => Guid.TryParseExact(dir.Name, "N", out _))
            .OrderBy(dir => Directory.GetLastWriteTime(dir.Path))
            .ToList();

        if (storageDirs.Count == 0)
        {
            return layout;
        }

        for (var i = 0; i < storageDirs.Count; i++)
        {
            var dir = storageDirs[i];
            var id = Guid.ParseExact(dir.Name, "N");
            var column = i % 2;
            var row = i / 2;
            var box = new BoxModel
            {
                Id = id,
                Title = GuessRecoveredBoxTitle(dir.Path, i),
                X = 80 + column * 450,
                Y = 80 + row * 330,
                Width = 400,
                Height = 280,
                ExpandedHeight = 280,
                SortOrder = i
            };
            BoxLayoutNormalizer.Normalize(box);
            layout.Boxes.Add(box);
        }

        _layoutStore.Save(layout);
        return layout;
    }

    private static string GuessRecoveredBoxTitle(string storagePath, int index)
    {
        try
        {
            var firstDirectory = Directory.EnumerateDirectories(storagePath)
                .Select(Path.GetFileName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
            if (!string.IsNullOrWhiteSpace(firstDirectory))
            {
                return firstDirectory;
            }
        }
        catch
        {
            // Keep recovery best-effort; broken item metadata should not block the box.
        }

        return $"恢复的盒子 {index + 1}";
    }
}
