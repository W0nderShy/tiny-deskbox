using System.Drawing;
using DeskBox.App.Helpers;
using DeskBox.Core.Models;
using WinForms = System.Windows.Forms;

namespace DeskBox.App.Services;

/// <summary>
/// WinForms 托盘。WPF 进程需隐藏 Form 宿主，否则 NotifyIcon 在 Win11 上可能不显示。
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly WinForms.Form _messageHost;
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly Icon _appIcon;
    private WinForms.ContextMenuStrip _menu = null!;

    public event EventHandler? NewBoxRequested;
    public event EventHandler? ToggleDesktopIconsRequested;
    public event EventHandler? ToggleStartupRequested;
    public event EventHandler? ToggleAutoCollapseRequested;
    public event EventHandler<int>? BoxOpacitySelected;
    public event EventHandler? ExitRequested;

    public TrayIconService()
    {
        WinForms.Application.EnableVisualStyles();
        WinForms.Application.SetCompatibleTextRenderingDefault(false);

        _appIcon = TrayIconHelper.CreateDeskBoxIcon();

        _messageHost = new WinForms.Form
        {
            ShowInTaskbar = false,
            FormBorderStyle = WinForms.FormBorderStyle.None,
            Size = new Size(0, 0),
            Opacity = 0,
            StartPosition = WinForms.FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            Text = "DeskBoxHost"
        };

        _notifyIcon = new WinForms.NotifyIcon
        {
            Text = "DeskBox 桌面收纳",
            Icon = _appIcon,
            Visible = false
        };

        _notifyIcon.DoubleClick += (_, _) => NewBoxRequested?.Invoke(this, EventArgs.Empty);
        RebuildMenu(new AppConfig());
    }

    public void EnsureVisible()
    {
        if (!_messageHost.IsHandleCreated)
        {
            _messageHost.Show();
            _messageHost.Hide();
        }

        _notifyIcon.Visible = true;
        _notifyIcon.Icon = _appIcon;
    }

    public void UpdateMenu(AppConfig config) => RebuildMenu(config);

    private void RebuildMenu(AppConfig config)
    {
        _menu?.Dispose();
        _menu = new WinForms.ContextMenuStrip();

        _menu.Items.Add("新建盒子", null, (_, _) => NewBoxRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(MenuLabel(config.HideDesktopIcons, "隐藏桌面图标"), null,
            (_, _) => ToggleDesktopIconsRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(MenuLabel(config.AutoCollapseEnabled, "鼠标离开自动折叠"), null,
            (_, _) => ToggleAutoCollapseRequested?.Invoke(this, EventArgs.Empty));

        var opacityMenu = new WinForms.ToolStripMenuItem($"盒子透明度 ({config.BoxOpacityPercent}%)");
        foreach (var level in new[] { 100, 85, 70, 55, 40 })
        {
            var item = new WinForms.ToolStripMenuItem($"{level}%{(level == 100 ? " 不透明" : "")}")
            {
                Checked = config.BoxOpacityPercent == level
            };
            var captured = level;
            item.Click += (_, _) => BoxOpacitySelected?.Invoke(this, captured);
            opacityMenu.DropDownItems.Add(item);
        }

        _menu.Items.Add(opacityMenu);

        _menu.Items.Add(MenuLabel(config.RunAtStartup, "开机自启"), null,
            (_, _) => ToggleStartupRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(new WinForms.ToolStripSeparator());
        _menu.Items.Add("退出", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon.ContextMenuStrip = _menu;
    }

    private static string MenuLabel(bool enabled, string text) => enabled ? $"✓ {text}" : text;

    public void ShowBalloon(string message) =>
        _notifyIcon.ShowBalloonTip(3000, "DeskBox", message, WinForms.ToolTipIcon.Info);

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _messageHost.Close();
        _messageHost.Dispose();
        _appIcon.Dispose();
    }
}
