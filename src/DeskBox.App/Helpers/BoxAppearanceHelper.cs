using System.Windows;
using System.Windows.Media;
using DeskBox.Core.Models;

namespace DeskBox.App.Helpers;

public static class BoxAppearanceHelper
{
    public const int MinOpacityPercent = 40;
    public const int MaxOpacityPercent = 100;

    public static int ClampOpacity(int percent) => Math.Clamp(percent, MinOpacityPercent, MaxOpacityPercent);

    public static byte ToAlpha(int opacityPercent) =>
        (byte)(ClampOpacity(opacityPercent) * 255 / 100);

    public static SolidColorBrush BrushFromColor(Color rgb, int opacityPercent) =>
        new(Color.FromArgb(ToAlpha(opacityPercent), rgb.R, rgb.G, rgb.B));

    public static void ApplyBoxOpacity(
        System.Windows.Controls.Border rootBorder,
        System.Windows.Controls.Border titleBar,
        AppConfig config,
        FrameworkElement resourceOwner,
        bool collapsed)
    {
        var surface = GetThemeColor(resourceOwner, "DeskBox.SurfaceColor", 0x2D, 0x2D, 0x30);
        var title = GetThemeColor(resourceOwner, "DeskBox.TitleBarColor", 0x3C, 0x3C, 0x3C);
        var border = GetThemeColor(resourceOwner, "DeskBox.BorderColor", 0x50, 0x50, 0x50);
        var alpha = ToAlpha(config.BoxOpacityPercent);
        var borderBrush = new SolidColorBrush(Color.FromArgb(alpha, border.R, border.G, border.B));

        if (collapsed)
        {
            rootBorder.Background = System.Windows.Media.Brushes.Transparent;
            rootBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
            titleBar.Background = BrushFromColor(title, config.BoxOpacityPercent);
            titleBar.BorderBrush = borderBrush;
            return;
        }

        rootBorder.Background = BrushFromColor(surface, config.BoxOpacityPercent);
        titleBar.Background = BrushFromColor(title, config.BoxOpacityPercent);
        rootBorder.BorderBrush = borderBrush;
        titleBar.BorderBrush = System.Windows.Media.Brushes.Transparent;
    }

    private static Color GetThemeColor(FrameworkElement owner, string key, byte r, byte g, byte b)
    {
        try
        {
            return owner.FindResource(key) is Color color ? color : Color.FromRgb(r, g, b);
        }
        catch (ResourceReferenceKeyNotFoundException)
        {
            return Color.FromRgb(r, g, b);
        }
    }
}
