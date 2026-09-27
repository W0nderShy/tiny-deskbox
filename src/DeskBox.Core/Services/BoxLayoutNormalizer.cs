using DeskBox.Core.Models;

namespace DeskBox.Core.Services;

public static class BoxLayoutNormalizer
{
    public const double TitleBarHeight = 32;
    public const double ExpandedMinHeight = TitleBarHeight + 48;

    public static void Normalize(BoxModel box)
    {
        if (box.Width < 160)
        {
            box.Width = 160;
        }

        var expandedHeight = Math.Max(box.ExpandedHeight, ExpandedMinHeight);
        if (box.Height > TitleBarHeight)
        {
            expandedHeight = Math.Max(expandedHeight, box.Height);
        }

        box.ExpandedHeight = expandedHeight;
        box.Height = expandedHeight;
    }

    public static void Normalize(LayoutDocument layout)
    {
        foreach (var box in layout.Boxes)
        {
            Normalize(box);
        }
    }
}
