namespace DeskBox.Core.Models;

public sealed class AppConfig
{
    public bool HideDesktopIcons { get; set; } = true;

    /// <summary>盒子背景不透明度 40–100（100 为完全不透明）。</summary>
    public int BoxOpacityPercent { get; set; } = 100;

    /// <summary>兼容旧配置；新安装请使用 BoxOpacityPercent。</summary>
    public bool OpaqueBoxBackground { get; set; } = true;

    public bool RunAtStartup { get; set; }
    public bool RestoreLayoutOnStart { get; set; } = true;

    /// <summary>鼠标离开盒子后自动收起到标题栏。</summary>
    public bool AutoCollapseEnabled { get; set; } = true;

    /// <summary>保留字段；当前为鼠标离开立即折叠。</summary>
    public int AutoCollapseDelayMs { get; set; }
}
