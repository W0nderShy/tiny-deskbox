namespace DeskBox.Core.Models;

public sealed class BoxModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "新盒子";
    public double X { get; set; } = 100;
    public double Y { get; set; } = 100;
    public double Width { get; set; } = 320;
    public double Height { get; set; } = 240;
    public double ExpandedHeight { get; set; } = 240;
    public bool Collapsed { get; set; }
    public bool AutoCollapsePaused { get; set; }
    public int SortOrder { get; set; }
}
