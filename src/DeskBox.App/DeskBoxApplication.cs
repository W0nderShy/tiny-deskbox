using System.Windows;
using DeskBox.App.Helpers;
using DeskBox.App.Services;
using DeskBox.App.Views;
using DeskBox.Core.Models;
using DeskBox.Core.Services;
using DeskBox.Shell;

namespace DeskBox.App;

public sealed class DeskBoxApplication : IDisposable
{
    private readonly ConfigStore _configStore = new();
    private readonly LayoutStore _layoutStore = new();
    private readonly BoxRepository _boxRepository;
    private readonly ShellDesktopService _shellDesktop = new();
    private readonly ShellFileOperations _fileOps;
    private readonly ShellIconLoader _iconLoader = new();
    private readonly TrayIconService _tray;
    private readonly Dictionary<Guid, BoxWindow> _windows = new();
    private readonly DebouncedAction _saveLayoutDebounced;
    private AppConfig _config;
    private bool _desktopIconsHiddenByApp;

    public DeskBoxApplication(TrayIconService tray)
    {
        _boxRepository = new BoxRepository(_layoutStore);
        _fileOps = new ShellFileOperations(_shellDesktop);
        _tray = tray;
        _config = _configStore.Load();

        _tray.NewBoxRequested += (_, _) => CreateNewBoxAtCenter();
        _tray.ToggleDesktopIconsRequested += (_, _) => ToggleDesktopIcons();
        _tray.ToggleStartupRequested += (_, _) => ToggleStartup();
        _tray.ToggleAutoCollapseRequested += (_, _) => ToggleAutoCollapse();
        _tray.BoxOpacitySelected += (_, percent) => SetBoxOpacity(percent);
        _tray.ExitRequested += (_, _) => Shutdown();
        _saveLayoutDebounced = new DebouncedAction(FlushAllBoxes);
        _tray.UpdateMenu(_config);
    }

    public void Start()
    {
        AppPaths.EnsureCreated();

        SyncStartupRegistry();

        var layout = _boxRepository.LoadLayout();
        if (layout.Boxes.Count == 0)
        {
            CreateNewBoxAtCenter();
        }
        else
        {
            foreach (var box in layout.Boxes.OrderBy(b => b.SortOrder))
            {
                OpenBox(box);
            }
        }

        ApplyDesktopIconVisibility();
        _tray.EnsureVisible();
    }

    private void ApplyDesktopIconVisibility()
    {
        if (!_config.HideDesktopIcons || _desktopIconsHiddenByApp || _windows.Count == 0)
        {
            return;
        }

        _shellDesktop.ToggleDesktopIcons();
        _desktopIconsHiddenByApp = true;
    }

    public void RestoreDesktopIconsIfHidden()
    {
        if (!_desktopIconsHiddenByApp)
        {
            return;
        }

        _shellDesktop.ToggleDesktopIcons();
        _desktopIconsHiddenByApp = false;
    }

    public void CreateNewBoxAtCenter()
    {
        var work = SystemParameters.WorkArea;
        var x = work.Left + (work.Width - 320) / 2;
        var y = work.Top + (work.Height - 240) / 2;
        var box = _boxRepository.CreateBox(x, y);
        OpenBox(box);
    }

    private void OpenBox(BoxModel model)
    {
        if (_windows.ContainsKey(model.Id))
        {
            return;
        }

        var window = new BoxWindow(model, _config, _boxRepository, _fileOps, _iconLoader, _shellDesktop);
        window.Closed += (_, _) =>
        {
            _windows.Remove(model.Id);
            PersistBox(window.Model);
        };
        window.ModelChanged += (_, _) => _saveLayoutDebounced.Invoke();
        window.LayoutCommitted += (_, _) => PersistBox(window.Model);
        window.RequestDelete += (_, restore) => DeleteBox(window, restore);

        _windows[model.Id] = window;
        window.Show();
        window.RefreshAutoCollapseSettings();
        window.RefreshAppearance();
        ApplyDesktopIconVisibility();
    }

    private void PersistBox(BoxModel model) => _boxRepository.UpdateBox(model);

    private void FlushAllBoxes()
    {
        foreach (var w in _windows.Values)
        {
            w.CommitLayoutToModel();
            PersistBox(w.Model);
        }
    }

    private void DeleteBox(BoxWindow window, bool restoreToDesktop)
    {
        if (restoreToDesktop)
        {
            var result = _fileOps.MoveAllBoxItemsToDesktop(window.Model.Id);
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "DeskBox", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        _boxRepository.DeleteBox(window.Model.Id, restoreToDesktop);
        window.Close();
        _tray.ShowBalloon("盒子已删除");
    }

    private void ToggleDesktopIcons()
    {
        _shellDesktop.ToggleDesktopIcons();
        _desktopIconsHiddenByApp = !_desktopIconsHiddenByApp;
        _config.HideDesktopIcons = _desktopIconsHiddenByApp;
        _configStore.Save(_config);
        _tray.UpdateMenu(_config);
    }

    private void ToggleAutoCollapse()
    {
        _config.AutoCollapseEnabled = !_config.AutoCollapseEnabled;
        _configStore.Save(_config);
        foreach (var w in _windows.Values)
        {
            w.RefreshAutoCollapseSettings();
        }

        _tray.UpdateMenu(_config);
        _tray.ShowBalloon(_config.AutoCollapseEnabled
            ? "已开启：鼠标离开盒子立即收起"
            : "已关闭自动折叠");
    }

    private void SetBoxOpacity(int percent)
    {
        _config.BoxOpacityPercent = BoxAppearanceHelper.ClampOpacity(percent);
        _config.OpaqueBoxBackground = _config.BoxOpacityPercent >= 99;
        _configStore.Save(_config);

        foreach (var w in _windows.Values)
        {
            w.RefreshAppearance();
        }

        _tray.UpdateMenu(_config);
        _tray.ShowBalloon($"盒子透明度已设为 {_config.BoxOpacityPercent}%");
    }

    private void ToggleStartup()
    {
        _config.RunAtStartup = !_config.RunAtStartup;
        SyncStartupRegistry();
        _configStore.Save(_config);
        _tray.UpdateMenu(_config);
        _tray.ShowBalloon(_config.RunAtStartup ? "已开启开机自启" : "已关闭开机自启");
    }

    private void SyncStartupRegistry()
    {
        var exe = Environment.ProcessPath ?? System.Windows.Application.ResourceAssembly.Location;
        if (string.IsNullOrEmpty(exe))
        {
            return;
        }

        StartupRegistryService.SetEnabled(_config.RunAtStartup, exe);
        if (_config.RunAtStartup != StartupRegistryService.IsEnabled())
        {
            _config.RunAtStartup = StartupRegistryService.IsEnabled();
            _configStore.Save(_config);
        }
    }

    public void Shutdown()
    {
        FlushAllBoxes();
        foreach (var w in _windows.Values.ToList())
        {
            w.Close();
        }

        RestoreDesktopIconsIfHidden();

        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        _iconLoader.Dispose();
        _tray.Dispose();
    }
}
