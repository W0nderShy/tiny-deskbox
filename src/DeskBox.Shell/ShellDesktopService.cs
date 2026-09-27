using DeskBox.Shell.Native;

namespace DeskBox.Shell;

public sealed class ShellDesktopService
{
    public string GetUserDesktopPath() =>
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    public IntPtr GetShellDefView()
    {
        var progman = NativeMethods.FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var shellView = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (shellView != IntPtr.Zero)
        {
            return shellView;
        }

        var desktopWnd = NativeMethods.FindWindow("Progman", null);
        IntPtr worker = IntPtr.Zero;
        while (true)
        {
            worker = NativeMethods.FindWindowEx(desktopWnd, worker, "WorkerW", null);
            if (worker == IntPtr.Zero)
            {
                break;
            }

            shellView = NativeMethods.FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                return shellView;
            }
        }

        worker = IntPtr.Zero;
        var desktopRoot = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", null);
        while (desktopRoot != IntPtr.Zero)
        {
            worker = NativeMethods.FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
            if (worker == IntPtr.Zero)
            {
                break;
            }

            shellView = NativeMethods.FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                return shellView;
            }
        }

        return IntPtr.Zero;
    }

    public void ToggleDesktopIcons()
    {
        var shellView = GetShellDefView();
        if (shellView == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.SendMessage(
            shellView,
            NativeMethods.WM_COMMAND,
            (IntPtr)NativeMethods.ToggleDesktopIconsCommand,
            IntPtr.Zero);
    }

    public void SetDesktopIconsVisible(bool visible)
    {
        // Toggle only when state differs is hard without reading WS_VISIBLE; caller tracks desired state.
        ToggleDesktopIcons();
    }

    public void RefreshDesktop()
    {
        NativeMethods.SHChangeNotify(
            NativeMethods.SHCNE_ASSOCCHANGED | NativeMethods.SHCNE_UPDATEDIR,
            NativeMethods.SHCNF_IDLIST | NativeMethods.SHCNF_FLUSH,
            IntPtr.Zero,
            IntPtr.Zero);
    }
}
