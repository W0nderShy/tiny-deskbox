using System.Windows.Media;
using DeskBox.Core.Models;

namespace DeskBox.App.Views;

public sealed class BoxItemViewModel
{
    public BoxItemViewModel(BoxItemInfo info, ImageSource? icon)
    {
        Info = info;
        Icon = icon;
    }

    public BoxItemInfo Info { get; }
    public string FullPath => Info.FullPath;
    public string DisplayName => Info.DisplayName;
    public bool IsDirectory => Info.IsDirectory;
    public ImageSource? Icon { get; }
}
