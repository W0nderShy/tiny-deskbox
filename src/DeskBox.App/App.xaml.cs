using System.Windows;
using System.Windows.Threading;
using DeskBox.App.Services;

namespace DeskBox.App;

public partial class App : System.Windows.Application
{
    private SingleInstanceService? _singleInstance;
    private TrayIconService? _tray;
    private DeskBoxApplication? _app;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "DeskBox 错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.IsFirstInstance)
        {
            MessageBox.Show("DeskBox 已在运行。", "DeskBox", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _tray = new TrayIconService();
        _app = new DeskBoxApplication(_tray);

        Dispatcher.BeginInvoke(() =>
        {
            _tray.EnsureVisible();
            _tray.ShowBalloon("DeskBox 已启动。若未看到图标，请在任务栏设置中打开 DeskBox 通知。");
        }, DispatcherPriority.ApplicationIdle);

        try
        {
            _app.Start();
        }
        catch (Exception ex)
        {
            _app.RestoreDesktopIconsIfHidden();
            MessageBox.Show(
                $"DeskBox 启动失败：{ex.Message}",
                "DeskBox",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            _app.Dispose();
            _singleInstance?.Dispose();
            Shutdown();
            return;
        }

        Exit += (_, _) =>
        {
            _app?.RestoreDesktopIconsIfHidden();
            _app?.Dispose();
            _singleInstance?.Dispose();
        };
    }
}
