using System.Text.Json;

namespace DeskBox.Core.Services;

internal static class AtomicJsonPersistence
{
    public const int SnapshotLimit = 10;
    private static long _lastSnapshotTicks;

    public static bool TryRead<T>(string path, JsonSerializerOptions options, out T? value)
    {
        value = default;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            value = JsonSerializer.Deserialize<T>(stream, options);
            return value is not null;
        }
        catch
        {
            return false;
        }
    }

    public static void Write<T>(string path, T value, JsonSerializerOptions options)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException($"Cannot determine the directory for {path}.");
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, options);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public static void WriteSnapshot<T>(
        string recoveryRoot,
        string prefix,
        T value,
        JsonSerializerOptions options)
    {
        Directory.CreateDirectory(recoveryRoot);
        var snapshotPath = Path.Combine(
            recoveryRoot,
            $"{prefix}-{NextSnapshotTicks():D19}-{Guid.NewGuid():N}.json");
        Write(snapshotPath, value, options);

        foreach (var oldSnapshot in EnumerateSnapshots(recoveryRoot, prefix).Skip(SnapshotLimit))
        {
            try
            {
                File.Delete(oldSnapshot);
            }
            catch
            {
                // A stale snapshot can be pruned on a later save.
            }
        }
    }

    public static IEnumerable<string> EnumerateSnapshots(string recoveryRoot, string prefix)
    {
        if (!Directory.Exists(recoveryRoot))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(recoveryRoot, $"{prefix}-*.json")
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
    }

    private static long NextSnapshotTicks()
    {
        while (true)
        {
            var previous = Interlocked.Read(ref _lastSnapshotTicks);
            var next = Math.Max(DateTime.UtcNow.Ticks, previous + 1);
            if (Interlocked.CompareExchange(ref _lastSnapshotTicks, next, previous) == previous)
            {
                return next;
            }
        }
    }
}
