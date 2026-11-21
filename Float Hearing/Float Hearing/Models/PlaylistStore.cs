using System.Collections.ObjectModel;
using System.ComponentModel;
using Float_Hearing.Services;

namespace Float_Hearing.Models;

/// <summary>歌单项（侧栏数据源；壳阶段为内存数据）。</summary>
public sealed class PlaylistItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string? _coverUri;

    public string Id { get; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
    }

    /// <summary>封面路径；空则侧栏显示占位图标。</summary>
    public string? CoverUri
    {
        get => _coverUri;
        set { _coverUri = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CoverUri))); }
    }

    public Microsoft.UI.Xaml.Visibility CoverVisibility =>
        string.IsNullOrEmpty(_coverUri) ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;

    public Microsoft.UI.Xaml.Visibility CoverPlaceholderVisibility =>
        string.IsNullOrEmpty(_coverUri) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>歌单集合：侧栏与操作共用；后续替换为数据库。</summary>
public static class PlaylistStore
{
    public static ObservableCollection<PlaylistItem> Items { get; } = [];

    static PlaylistStore()
    {
        Items.Add(new PlaylistItem { Name = Loc.Get("PlaylistDemoFavorites") });
        Items.Add(new PlaylistItem { Name = Loc.Get("PlaylistDemoSleep") });
        Items.Add(new PlaylistItem { Name = Loc.Get("PlaylistDemoRoad") });
        Items.Add(new PlaylistItem { Name = Loc.Get("PlaylistDemoFocus") });
    }

    public static PlaylistItem AddNew()
    {
        var item = new PlaylistItem { Name = $"{Loc.Get("PlaylistNewPrefix")} {Items.Count + 1}" };
        Items.Add(item);
        return item;
    }

    public static void Move(PlaylistItem item, int newIndex)
    {
        var oldIndex = Items.IndexOf(item);
        if (oldIndex < 0 || newIndex < 0 || newIndex >= Items.Count || oldIndex == newIndex)
        {
            return;
        }

        Items.Move(oldIndex, newIndex);
    }

    public static int IndexOf(PlaylistItem item) => Items.IndexOf(item);
}
