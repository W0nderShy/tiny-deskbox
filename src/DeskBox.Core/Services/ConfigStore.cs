using System.Text.Json;
using DeskBox.Core.Models;

namespace DeskBox.Core.Services;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _configFile;
    private readonly string _recoveryRoot;

    public ConfigStore()
        : this(AppPaths.ConfigFile, AppPaths.RecoveryRoot)
    {
    }

    public ConfigStore(string configFile, string recoveryRoot)
    {
        _configFile = configFile;
        _recoveryRoot = recoveryRoot;
    }

    public AppConfig Load()
    {
        EnsureCreated();
        if (TryReadConfig(_configFile, out var config))
        {
            if (!AtomicJsonPersistence.EnumerateSnapshots(_recoveryRoot, "config").Any())
            {
                AtomicJsonPersistence.WriteSnapshot(_recoveryRoot, "config", config, JsonOptions);
            }

            return config;
        }

        foreach (var snapshotPath in AtomicJsonPersistence.EnumerateSnapshots(_recoveryRoot, "config"))
        {
            if (!TryReadConfig(snapshotPath, out var recovered))
            {
                continue;
            }

            AtomicJsonPersistence.Write(_configFile, recovered, JsonOptions);
            return recovered;
        }

        return new AppConfig();
    }

    public void Save(AppConfig config)
    {
        EnsureCreated();
        Normalize(config);
        AtomicJsonPersistence.Write(_configFile, config, JsonOptions);
        AtomicJsonPersistence.WriteSnapshot(_recoveryRoot, "config", config, JsonOptions);
    }

    private static bool TryReadConfig(string path, out AppConfig config)
    {
        if (!AtomicJsonPersistence.TryRead<AppConfig>(path, JsonOptions, out var loaded)
            || loaded is null)
        {
            config = new AppConfig();
            return false;
        }

        Normalize(loaded);
        config = loaded;
        return true;
    }

    private void EnsureCreated()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configFile)!);
        Directory.CreateDirectory(_recoveryRoot);
    }

    private static void Normalize(AppConfig config)
    {
        if (config.BoxOpacityPercent is < 40 or > 100)
        {
            config.BoxOpacityPercent = config.OpaqueBoxBackground ? 100 : 80;
        }

        config.OpaqueBoxBackground = config.BoxOpacityPercent >= 99;
    }
}
