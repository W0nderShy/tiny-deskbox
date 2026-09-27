using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using DeskBox.App.Helpers;
using DeskBox.Core.Models;
using DeskBox.Core.Services;
using DeskBox.Shell;

namespace DeskBox.App.Views;

public partial class BoxWindow : Window
{
    private const double TitleBarHeight = 32;
    private const double ExpandedMinHeight = TitleBarHeight + 48;
    private bool _isResizing;
    private readonly BoxRepository _boxRepository;
    private readonly ShellFileOperations _fileOps;
    private readonly ShellIconLoader _iconLoader;
    private readonly ShellDesktopService _shellDesktop;
    private readonly AppConfig _config;
    private readonly ObservableCollection<BoxItemViewModel> _items = new();
    private Point _dragStart;
    private bool _isDraggingWindow;
    private bool _suppressAutoCollapse;
    private Point _itemDragStart;
    private BoxItemViewModel? _pendingDragItem;
    private DateTime _autoCollapseSuppressedUntil;
    private DispatcherTimer? _autoCollapseRecheckTimer;
    private bool _isApplyingLayout;
    private double _rememberedWidth;
    private double _rememberedHeight;
    private bool _hoverCollapseActive;

    public BoxModel Model { get; }
    public event EventHandler? ModelChanged;
    public event EventHandler? LayoutCommitted;
    public event EventHandler<bool>? RequestDelete;

    public BoxWindow(
        BoxModel model,
        AppConfig config,
        BoxRepository boxRepository,
        ShellFileOperations fileOps,
        ShellIconLoader iconLoader,
        ShellDesktopService shellDesktop)
    {
        InitializeComponent();
        Model = model;
        _config = config;
        _boxRepository = boxRepository;
        _fileOps = fileOps;
        _iconLoader = iconLoader;
        _shellDesktop = shellDesktop;

        _rememberedWidth = Math.Max(model.Width, MinWidth);
        _rememberedHeight = Math.Max(Math.Max(model.ExpandedHeight, model.Height), ExpandedMinHeight);
        _hoverCollapseActive = config.AutoCollapseEnabled && !model.AutoCollapsePaused;

        NativeWindowHelper.ApplyDesktopBoxStyles(this);
        ApplyAppearance();
        ApplyBounds();
        RefreshItems();

        ItemsList.ItemsSource = _items;
        TitleText.Text = Model.Title;
        ItemsList.PreviewMouseRightButtonUp += ItemsList_PreviewMouseRightButtonUp;

        UpdateCollapseButtonHint();
        LocationChanged += (_, _) => OnGeometryChanged();
        SizeChanged += (_, _) => OnGeometryChanged();
    }

    public void RefreshAutoCollapseSettings()
    {
        _hoverCollapseActive = _config.AutoCollapseEnabled && !Model.AutoCollapsePaused;
        UpdateCollapseButtonHint();
    }

    public void RefreshAppearance() => ApplyAppearance();

    private void ApplyAppearance() =>
        BoxAppearanceHelper.ApplyBoxOpacity(RootBorder, TitleBar, _config, this, Model.Collapsed);

    private void ApplyBounds()
    {
        Left = Model.X;
        Top = Model.Y;
        Width = Model.Width;
        if (Model.Collapsed)
        {
            SetCollapsedVisual();
        }
        else
        {
            ApplyExpandedLayout();
        }

        UpdateResizeChrome();
    }

    public void CommitLayoutToModel()
    {
        Model.X = Left;
        Model.Y = Top;
        Model.Width = _rememberedWidth;

        if (!Model.Collapsed && Height > TitleBarHeight)
        {
            RememberExpandedSize(Width, Height);
        }

        Model.Height = _rememberedHeight;
        Model.ExpandedHeight = _rememberedHeight;
    }

    private void RememberExpandedSize(double width, double height)
    {
        _rememberedWidth = Math.Max(width, MinWidth);
        _rememberedHeight = Math.Max(height, ExpandedMinHeight);
        Model.Width = _rememberedWidth;
        Model.Height = _rememberedHeight;
        Model.ExpandedHeight = _rememberedHeight;
    }

    private void OnGeometryChanged()
    {
        if (_isApplyingLayout)
        {
            return;
        }

        Model.X = Left;
        Model.Y = Top;

        if (Model.Collapsed)
        {
            if (Math.Abs(Height - TitleBarHeight) > 0.5)
            {
                Height = TitleBarHeight;
            }

            Model.Width = _rememberedWidth;
            ModelChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (Height <= TitleBarHeight)
        {
            return;
        }

        Model.Width = Width;
        RememberExpandedSize(Width, Height);
        ModelChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CommitLayoutNow()
    {
        CommitLayoutToModel();
        LayoutCommitted?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshItems()
    {
        _items.Clear();
        foreach (var info in _boxRepository.ListItems(Model.Id))
        {
            var icon = _iconLoader.GetIcon(info.FullPath, info.IsDirectory);
            _items.Add(new BoxItemViewModel(info, IconBitmapHelper.FromIcon(icon)));
        }
    }

    private void BoxWindow_MouseEnter(object sender, MouseEventArgs e)
    {
        if (_config.AutoCollapseEnabled && _hoverCollapseActive && Model.Collapsed)
        {
            Expand();
        }
    }

    private void BoxWindow_MouseLeave(object sender, MouseEventArgs e) => ScheduleAutoCollapseCheck();

    private void CollapseButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _autoCollapseRecheckTimer?.Stop();

        if (!_config.AutoCollapseEnabled)
        {
            if (Model.Collapsed)
            {
                Expand();
            }
            else
            {
                Collapse();
            }

            return;
        }

        if (_hoverCollapseActive)
        {
            _hoverCollapseActive = false;
            Model.AutoCollapsePaused = true;
            _suppressAutoCollapse = true;
            if (Model.Collapsed)
            {
                Expand();
            }
            else
            {
                Collapse();
            }
        }
        else
        {
            _hoverCollapseActive = true;
            Model.AutoCollapsePaused = false;
            _suppressAutoCollapse = false;
            ScheduleAutoCollapseCheck();
        }

        UpdateCollapseButtonHint();
        CommitLayoutNow();
    }

    private void UpdateCollapseButtonHint()
    {
        if (!_config.AutoCollapseEnabled)
        {
            CollapseButton.ToolTip = "收起/展开";
            CollapseButton.Content = "−";
            return;
        }

        if (_hoverCollapseActive)
        {
            CollapseButton.ToolTip = "固定展开/收起（暂停移开自动折叠）";
            CollapseButton.Content = "−";
        }
        else
        {
            CollapseButton.ToolTip = "恢复移开自动折叠";
            CollapseButton.Content = "▪";
        }
    }

    private void Collapse()
    {
        if (Model.Collapsed)
        {
            return;
        }

        if (Height > TitleBarHeight)
        {
            RememberExpandedSize(Width, Height);
        }

        Model.Collapsed = true;
        SetCollapsedVisual();
        CommitLayoutNow();
    }

    private void Expand()
    {
        if (!Model.Collapsed)
        {
            return;
        }

        Model.Collapsed = false;
        _autoCollapseSuppressedUntil = DateTime.UtcNow.AddMilliseconds(200);
        ApplyExpandedLayout();
        ApplyAppearance();
        CommitLayoutNow();
    }

    private void SetCollapsedVisual()
    {
        _isApplyingLayout = true;
        try
        {
            ContentRow.Height = new GridLength(0);
            ItemsList.Visibility = Visibility.Collapsed;
            TitleBar.CornerRadius = new CornerRadius(8);
            RootBorder.BorderThickness = new Thickness(0);
            TitleBar.BorderThickness = new Thickness(1);
            MinHeight = TitleBarHeight;
            Height = TitleBarHeight;
            ApplyAppearance();
            UpdateResizeChrome();
        }
        finally
        {
            _isApplyingLayout = false;
        }
    }

    private void ApplyExpandedLayout()
    {
        _isApplyingLayout = true;
        try
        {
            ContentRow.Height = new GridLength(1, GridUnitType.Star);
            ItemsList.Visibility = Visibility.Visible;
            TitleBar.CornerRadius = new CornerRadius(8, 8, 0, 0);
            RootBorder.BorderThickness = new Thickness(1);
            TitleBar.BorderThickness = new Thickness(0);
            MinHeight = ExpandedMinHeight;

            var targetWidth = Math.Max(_rememberedWidth, MinWidth);
            var targetHeight = Math.Max(_rememberedHeight, ExpandedMinHeight);

            Width = targetWidth;
            Height = targetHeight;
            RememberExpandedSize(targetWidth, targetHeight);
            UpdateResizeChrome();
        }
        finally
        {
            _isApplyingLayout = false;
        }
    }

    private void UpdateResizeChrome() =>
        ResizeChrome.Visibility = Model.Collapsed ? Visibility.Collapsed : Visibility.Visible;

    private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        _isResizing = true;
        _suppressAutoCollapse = true;
    }

    private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        _isResizing = false;
        _suppressAutoCollapse = false;
        CommitLayoutNow();
        ScheduleAutoCollapseCheck();
    }

    private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (Model.Collapsed)
        {
            return;
        }

        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        SyncExpandedSizeToModel();
    }

    private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (Model.Collapsed)
        {
            return;
        }

        Height = Math.Max(MinHeight, Height + e.VerticalChange);
        SyncExpandedSizeToModel();
    }

    private void ResizeCorner_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (Model.Collapsed)
        {
            return;
        }

        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        Height = Math.Max(MinHeight, Height + e.VerticalChange);
        SyncExpandedSizeToModel();
    }

    private void SyncExpandedSizeToModel()
    {
        if (Height <= TitleBarHeight)
        {
            return;
        }

        RememberExpandedSize(Width, Height);
    }

    private void ScheduleAutoCollapseCheck()
    {
        Dispatcher.BeginInvoke(TryAutoCollapse, DispatcherPriority.Input);

        if (DateTime.UtcNow < _autoCollapseSuppressedUntil)
        {
            var waitMs = (int)(_autoCollapseSuppressedUntil - DateTime.UtcNow).TotalMilliseconds + 50;
            ScheduleAutoCollapseRecheck(Math.Max(waitMs, 50));
        }
    }

    private void ScheduleAutoCollapseRecheck(int delayMs)
    {
        _autoCollapseRecheckTimer?.Stop();
        _autoCollapseRecheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delayMs) };
        _autoCollapseRecheckTimer.Tick += (_, _) =>
        {
            _autoCollapseRecheckTimer?.Stop();
            TryAutoCollapse();
        };
        _autoCollapseRecheckTimer.Start();
    }

    private void TryAutoCollapse()
    {
        if (DateTime.UtcNow < _autoCollapseSuppressedUntil)
        {
            ScheduleAutoCollapseRecheck(
                (int)(_autoCollapseSuppressedUntil - DateTime.UtcNow).TotalMilliseconds + 50);
            return;
        }

        if (!_config.AutoCollapseEnabled || !_hoverCollapseActive || Model.Collapsed || _suppressAutoCollapse
            || _isDraggingWindow || _isResizing)
        {
            return;
        }

        if (!IsMouseOver)
        {
            Collapse();
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            return;
        }

        _dragStart = e.GetPosition(this);
        _isDraggingWindow = true;
        _suppressAutoCollapse = true;
        CaptureMouse();
    }

    private void TitleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
        {
            TitleBar_MouseLeftButtonDown(TitleBar, e);
            return;
        }

        e.Handled = true;
        var dlg = new RenameDialog(Model.Title, this) { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.BoxName))
        {
            Model.Title = dlg.BoxName;
            TitleText.Text = Model.Title;
            ModelChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_isDraggingWindow)
        {
            _isDraggingWindow = false;
            _suppressAutoCollapse = false;
            ReleaseMouseCapture();
            ScheduleAutoCollapseCheck();
        }

        base.OnMouseLeftButtonUp(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isDraggingWindow && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(this);
            Left += pos.X - _dragStart.X;
            Top += pos.Y - _dragStart.Y;
        }

        base.OnMouseMove(e);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _suppressAutoCollapse = true;
        var result = MessageBox.Show(
            "删除盒子时是否将其中文件放回桌面？\n\n是 = 放回桌面\n否 = 仅删除盒子（文件一并删除）\n取消 = 不删除",
            "删除盒子",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            _suppressAutoCollapse = false;
            return;
        }

        RequestDelete?.Invoke(this, result == MessageBoxResult.Yes);
        _suppressAutoCollapse = false;
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (!_isDraggingWindow)
        {
            _suppressAutoCollapse = false;
            ScheduleAutoCollapseCheck();
        }
    }

    private void Content_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private string ResolveDropDestination(DragEventArgs e)
    {
        var boxRoot = Path.GetFullPath(AppPaths.BoxStorage(Model.Id));
        var pos = e.GetPosition(ItemsList);
        if (ItemsList.InputHitTest(pos) is DependencyObject hit)
        {
            var vm = GetItemFromSource(hit);
            if (vm is not null && vm.IsDirectory)
            {
                var folder = Path.GetFullPath(vm.FullPath);
                if (folder.StartsWith(boxRoot, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(folder, boxRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return folder;
                }
            }
        }

        return boxRoot;
    }

    private void Content_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        var paths = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        if (paths is null || paths.Length == 0)
        {
            return;
        }

        var destination = ResolveDropDestination(e);
        var result = _fileOps.MoveItems(paths, destination);
        if (!result.Success)
        {
            MessageBox.Show(result.ErrorMessage, "DeskBox", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        RefreshItems();
        _shellDesktop.RefreshDesktop();
        ScheduleAutoCollapseCheck();
    }

    private void ItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (GetItemFromSource(e.OriginalSource) is { } vm)
        {
            ShellItemLauncher.Open(vm.FullPath);
        }
    }

    private void ItemsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pendingDragItem = GetItemFromSource(e.OriginalSource);
        _itemDragStart = e.GetPosition(ItemsList);
    }

    private void ItemsList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _pendingDragItem is null)
        {
            return;
        }

        var pos = e.GetPosition(ItemsList);
        if (Math.Abs(pos.X - _itemDragStart.X) < 6 && Math.Abs(pos.Y - _itemDragStart.Y) < 6)
        {
            return;
        }

        var vm = _pendingDragItem;
        _pendingDragItem = null;
        _suppressAutoCollapse = true;
        var data = new DataObject(DataFormats.FileDrop, new[] { vm.FullPath });
        var effects = DragDrop.DoDragDrop(ItemsList, data, DragDropEffects.Move);
        _suppressAutoCollapse = false;
        if (effects == DragDropEffects.Move)
        {
            RefreshItems();
            _shellDesktop.RefreshDesktop();
        }

        ScheduleAutoCollapseCheck();
    }

    private static BoxItemViewModel? GetItemFromSource(object? source)
    {
        while (source is DependencyObject d)
        {
            if (d is FrameworkElement { DataContext: BoxItemViewModel vm })
            {
                return vm;
            }

            source = VisualTreeHelper.GetParent(d);
        }

        return null;
    }

    private void ItemsList_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (GetItemFromSource(e.OriginalSource) is not { } vm)
        {
            return;
        }

        e.Handled = true;
        var menu = BuildItemContextMenu(vm);
        menu.PlacementTarget = ItemsList;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.Closed += (_, _) => ScheduleAutoCollapseCheck();
        menu.IsOpen = true;
    }

    private ContextMenu BuildItemContextMenu(BoxItemViewModel vm)
    {
        var menu = new ContextMenu
        {
            Background = (System.Windows.Media.Brush)FindResource("DeskBox.TitleBarBrush"),
            Foreground = (System.Windows.Media.Brush)FindResource("DeskBox.TextPrimaryBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("DeskBox.BorderBrush")
        };
        AddMenu(menu, "打开", () => ShellItemLauncher.Open(vm.FullPath));
        AddMenu(menu, "打开所在位置", () => ShellItemLauncher.ShowInFolder(vm.FullPath));
        AddMenu(menu, "放回桌面", () =>
        {
            var result = _fileOps.MoveToDesktop(new[] { vm.FullPath });
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "DeskBox", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RefreshItems();
        });
        menu.Items.Add(new Separator());
        AddMenu(menu, "从盒子移除并删除", () =>
        {
            if (MessageBox.Show($"确定删除「{vm.DisplayName}」？", "DeskBox", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                if (File.Exists(vm.FullPath))
                {
                    File.Delete(vm.FullPath);
                }
                else if (Directory.Exists(vm.FullPath))
                {
                    Directory.Delete(vm.FullPath, true);
                }

                RefreshItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DeskBox", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        });
        return menu;
    }

    private static void AddMenu(ContextMenu menu, string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }
}
