using System.Text.Json;
using DeskBox.Core.Models;

namespace DeskBox.Core.Services;

public sealed class LayoutStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _layoutFile;
    private readonly string _storageRoot;
    private readonly string _recoveryRoot;

    public LayoutStore()
        : this(AppPaths.LayoutFile, AppPaths.StorageRoot, AppPaths.RecoveryRoot)
    {
    }

    public LayoutStore(string layoutFile, string storageRoot, string recoveryRoot)
    {
        _layoutFile = layoutFile;
        _storageRoot = storageRoot;
        _recoveryRoot = recoveryRoot;
    }

    public LayoutDocument Load()
    {
        EnsureCreated();
        var storageIds = GetStorageBoxIds();
        if (TryReadLayout(_layoutFile, out var layout)
            && (layout.Boxes.Count > 0 || storageIds.Count == 0))
        {
            AtomicJsonPersistence.Write(_layoutFile, layout, JsonOptions);
            if (!AtomicJsonPersistence.EnumerateSnapshots(_recoveryRoot, "layout").Any())
            {
                AtomicJsonPersistence.WriteSnapshot(_recoveryRoot, "layout", layout, JsonOptions);
            }

            return layout;
        }

        foreach (var snapshotPath in AtomicJsonPersistence.EnumerateSnapshots(_recoveryRoot, "layout"))
        {
            if (!TryReadLayout(snapshotPath, out var recovered))
            {
                continue;
            }

            recovered.Boxes.RemoveAll(box => !storageIds.Contains(box.Id));
            if (recovered.Boxes.Count == 0)
            {
                continue;
            }

            BoxLayoutNormalizer.Normalize(recovered);
            AtomicJsonPersistence.Write(_layoutFile, recovered, JsonOptions);
            return recovered;
        }

        return new LayoutDocument();
    }

    public void Save(LayoutDocument layout)
    {
        EnsureCreated();
        BoxLayoutNormalizer.Normalize(layout);
        AtomicJsonPersistence.Write(_layoutFile, layout, JsonOptions);
        AtomicJsonPersistence.WriteSnapshot(_recoveryRoot, "layout", layout, JsonOptions);
    }

    private bool TryReadLayout(string path, out LayoutDocument layout)
    {
        if (!AtomicJsonPersistence.TryRead<LayoutDocument>(path, JsonOptions, out var loaded)
            || loaded?.Boxes is null)
        {
            layout = new LayoutDocument();
            return false;
        }

        BoxLayoutNormalizer.Normalize(loaded);
        layout = loaded;
        return true;
    }

    private HashSet<Guid> GetStorageBoxIds()
    {
        if (!Directory.Exists(_storageRoot))
        {
            return new HashSet<Guid>();
        }

        return Directory.EnumerateDirectories(_storageRoot)
            .Select(Path.GetFileName)
            .Select(name => Guid.TryParseExact(name, "N", out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
    }

    private void EnsureCreated()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_layoutFile)!);
        Directory.CreateDirectory(_storageRoot);
        Directory.CreateDirectory(_recoveryRoot);
    }
}
