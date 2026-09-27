using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace DeskBox.App.Helpers;

public static class TrayIconHelper
{
    public static Icon CreateDeskBoxIcon()
    {
        using var bitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            using var bg = new SolidBrush(System.Drawing.Color.FromArgb(45, 45, 48));
            g.FillEllipse(bg, 1, 1, 30, 30);

            using var accent = new SolidBrush(System.Drawing.Color.FromArgb(74, 158, 255));
            g.FillRectangle(accent, 8, 11, 16, 13);
            g.FillRectangle(accent, 10, 8, 12, 4);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
