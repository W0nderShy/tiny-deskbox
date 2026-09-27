using System.Text.Json;
using DeskBox.Core.Models;
using DeskBox.Core.Services;
using Xunit;

namespace DeskBox.Core.Tests;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"DeskBoxTests-{Guid.NewGuid():N}");

    [Fact]
    public void CorruptPrimaryRecoversNewestConfigurationSnapshot()
    {
        var paths = TestPaths.Create(_root);
        var store = new ConfigStore(paths.ConfigFile, paths.RecoveryRoot);
        store.Save(new AppConfig { BoxOpacityPercent = 60, AutoCollapseEnabled = true });
        store.Save(new AppConfig { BoxOpacityPercent = 85, AutoCollapseEnabled = false });
        File.WriteAllText(paths.ConfigFile, "{broken");

        var loaded = store.Load();

        Assert.Equal(85, loaded.BoxOpacityPercent);
        Assert.False(loaded.AutoCollapseEnabled);
        JsonDocument.Parse(File.ReadAllText(paths.ConfigFile));
        Assert.Equal(2, Directory.EnumerateFiles(paths.RecoveryRoot, "config-*.json").Count());
    }

    [Fact]
    public void LoadingExistingPrimaryCreatesBaselineSnapshot()
    {
        var paths = TestPaths.Create(_root);
        File.WriteAllText(
            paths.ConfigFile,
            JsonSerializer.Serialize(new AppConfig { BoxOpacityPercent = 75, AutoCollapseEnabled = false }));
        var store = new ConfigStore(paths.ConfigFile, paths.RecoveryRoot);

        store.Load();

        Assert.Single(Directory.EnumerateFiles(paths.RecoveryRoot, "config-*.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
