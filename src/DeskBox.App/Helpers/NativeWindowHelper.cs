using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DeskBox.Shell.Native;

namespace DeskBox.App.Helpers;

public static class NativeWindowHelper
{
    public static void ApplyDesktopBoxStyles(Window window)
    {
        window.ShowInTaskbar = false;
        window.Topmost = false;

        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);
        };
    }
}
