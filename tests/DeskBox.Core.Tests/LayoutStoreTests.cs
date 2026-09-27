using System.Text.Json;
using DeskBox.Core.Models;
using DeskBox.Core.Services;
using Xunit;

namespace DeskBox.Core.Tests;

public sealed class LayoutStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"DeskBoxTests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoadRoundTripsCompleteBoxState()
    {
        var paths = TestPaths.Create(_root);
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        paths.CreateBoxStorage(id);
        var store = new LayoutStore(paths.LayoutFile, paths.StorageRoot, paths.RecoveryRoot);
        var layout = CreateLayout(id, "常用", collapsed: true, autoCollapsePaused: true);

        store.Save(layout);
        var loaded = store.Load();

        var box = Assert.Single(loaded.Boxes);
        Assert.Equal("常用", box.Title);
        Assert.Equal(123, box.X);
        Assert.Equal(234, box.Y);
        Assert.Equal(456, box.Width);
        Assert.Equal(345, box.ExpandedHeight);
        Assert.True(box.Collapsed);
        Assert.True(box.AutoCollapsePaused);
        Assert.Equal(2, box.SortOrder);
        Assert.Empty(Directory.EnumerateFiles(paths.Root, "*.tmp", SearchOption.AllDirectories));
        JsonDocument.Parse(File.ReadAllText(paths.LayoutFile));
    }

    [Fact]
    public void CorruptPrimaryRecoversNewestValidSnapshot()
    {
        var paths = TestPaths.Create(_root);
        var id = Guid.NewGuid();
        paths.CreateBoxStorage(id);
        var store = new LayoutStore(paths.LayoutFile, paths.StorageRoot, paths.RecoveryRoot);
        store.Save(CreateLayout(id, "旧位置"));
        store.Save(CreateLayout(id, "最新位置"));
        File.WriteAllText(paths.LayoutFile, "{broken");

        var loaded = store.Load();

        Assert.Equal("最新位置", Assert.Single(loaded.Boxes).Title);
        Assert.Equal("最新位置", Assert.Single(ReadPrimary(paths.LayoutFile).Boxes).Title);
    }

    [Fact]
    public void EmptyPrimaryWithStorageRecoversNewestSnapshot()
    {
        var paths = TestPaths.Create(_root);
        var id = Guid.NewGuid();
        paths.CreateBoxStorage(id);
        var store = new LayoutStore(paths.LayoutFile, paths.StorageRoot, paths.RecoveryRoot);
        store.Save(CreateLayout(id, "保留位置"));
        File.WriteAllText(paths.LayoutFile, "{\"Boxes\":[]}");

        var loaded = store.Load();

        Assert.Equal("保留位置", Assert.Single(loaded.Boxes).Title);
    }

    [Fact]
    public void RecoveryDoesNotRestoreBoxesWhoseStorageWasDeleted()
    {
        var paths = TestPaths.Create(_root);
        var keptId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        paths.CreateBoxStorage(keptId);
        paths.CreateBoxStorage(deletedId);
        var store = new LayoutStore(paths.LayoutFile, paths.StorageRoot, paths.RecoveryRoot);
        var layout = CreateLayout(keptId, "保留");
        layout.Boxes.Add(CreateLayout(deletedId, "已删除").Boxes[0]);
        store.Save(layout);
        Directory.Delete(Path.Combine(paths.StorageRoot, deletedId.ToString("N")), true);
        File.WriteAllText(paths.LayoutFile, "{broken");

        var loaded = store.Load();

        var box = Assert.Single(loaded.Boxes);
        Assert.Equal(keptId, box.Id);
    }

    [Fact]
    public void SaveRetainsOnlyTenNewestSnapshots()
    {
        var paths = TestPaths.Create(_root);
        var id = Guid.NewGuid();
        paths.CreateBoxStorage(id);
        var store = new LayoutStore(paths.LayoutFile, paths.StorageRoot, paths.RecoveryRoot);

        for (var i = 0; i < 12; i++)
        {
            store.Save(CreateLayout(id, $"版本 {i}"));
        }

        Assert.Equal(10, Directory.EnumerateFiles(paths.RecoveryRoot, "layout-*.json").Count());
    }

    [Fact]
    public void LoadingExistingPrimaryCreatesBaselineSnapshot()
    {
        var paths = TestPaths.Create(_root);
        var id = Guid.NewGuid();
        paths.CreateBoxStorage(id);
        File.WriteAllText(paths.LayoutFile, JsonSerializer.Serialize(CreateLayout(id, "升级前布局")));
        var store = new LayoutStore(paths.LayoutFile, paths.StorageRoot, paths.RecoveryRoot);

        store.Load();

        Assert.Single(Directory.EnumerateFiles(paths.RecoveryRoot, "layout-*.json"));
    }

    [Fact]
    public void LoadingLegacyPrimaryWritesCurrentSchema()
    {
        var paths = TestPaths.Create(_root);
        var id = Guid.NewGuid();
        paths.CreateBoxStorage(id);
        File.WriteAllText(
            paths.LayoutFile,
            $$"""
            {"Boxes":[{"Id":"{{id}}","Title":"旧格式","X":10,"Y":20,"Width":320,"Height":240,"ExpandedHeight":240,"Collapsed":false,"SortOrder":0}]}
            """);
        var store = new LayoutStore(paths.LayoutFile, paths.StorageRoot, paths.RecoveryRoot);

        store.Load();

        Assert.Contains("AutoCollapsePaused", File.ReadAllText(paths.LayoutFile));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    private static LayoutDocument CreateLayout(
        Guid id,
        string title,
        bool collapsed = false,
        bool autoCollapsePaused = false) =>
        new()
        {
            Boxes =
            {
                new BoxModel
                {
                    Id = id,
                    Title = title,
                    X = 123,
                    Y = 234,
                    Width = 456,
                    Height = 345,
                    ExpandedHeight = 345,
                    Collapsed = collapsed,
                    AutoCollapsePaused = autoCollapsePaused,
                    SortOrder = 2
                }
            }
        };

    private static LayoutDocument ReadPrimary(string path) =>
        JsonSerializer.Deserialize<LayoutDocument>(File.ReadAllText(path))!;
}

internal sealed record TestPaths(string Root, string LayoutFile, string ConfigFile, string StorageRoot, string RecoveryRoot)
{
    public static TestPaths Create(string root)
    {
        var paths = new TestPaths(
            root,
            Path.Combine(root, "layout.json"),
            Path.Combine(root, "config.json"),
            Path.Combine(root, "storage"),
            Path.Combine(root, "recovery"));
        Directory.CreateDirectory(paths.StorageRoot);
        Directory.CreateDirectory(paths.RecoveryRoot);
        return paths;
    }

    public void CreateBoxStorage(Guid id) =>
        Directory.CreateDirectory(Path.Combine(StorageRoot, id.ToString("N")));
}
