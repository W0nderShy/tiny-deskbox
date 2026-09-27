using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeskBox.App.Helpers;

public static class IconBitmapHelper
{
    public static ImageSource? FromIcon(Icon? icon)
    {
        if (icon is null)
        {
            return null;
        }

        return Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
    }
}
