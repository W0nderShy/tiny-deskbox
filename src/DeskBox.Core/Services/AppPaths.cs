namespace DeskBox.Core.Services;

public static class AppPaths
{
    public static string Root =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeskBox");

    public static string ConfigFile => Path.Combine(Root, "config.json");
    public static string LayoutFile => Path.Combine(Root, "layout.json");
    public static string StorageRoot => Path.Combine(Root, "storage");
    public static string RecoveryRoot => Path.Combine(Root, "recovery");

    public static string BoxStorage(Guid boxId) => Path.Combine(StorageRoot, boxId.ToString("N"));

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(StorageRoot);
        Directory.CreateDirectory(RecoveryRoot);
    }
}
