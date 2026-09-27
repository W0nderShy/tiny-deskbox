namespace DeskBox.Core.Models;

public sealed class BoxItemInfo
{
    public required string FullPath { get; init; }
    public required string DisplayName { get; init; }
    public bool IsDirectory { get; init; }
}
