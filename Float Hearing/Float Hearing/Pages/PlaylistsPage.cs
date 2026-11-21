using System.Collections.ObjectModel;
using Float_Hearing.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Float_Hearing.Pages;

/// <summary>歌单项（壳阶段：内存演示数据，后续接数据库）。</summary>
public sealed class PlaylistItem
{
    public string Name { get; set; } = string.Empty;

    public string? CoverUri { get; set; }

    public Visibility CoverVisibility => string.IsNullOrEmpty(CoverUri) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility CoverPlaceholderVisibility => string.IsNullOrEmpty(CoverUri) ? Visibility.Visible : Visibility.Collapsed;
}

/// <summary>歌单页：添加、封面网格、拖拽排序、右键菜单。</summary>
public sealed partial class PlaylistsPage : Page
{
    public PlaylistsPage()
    {
        InitializeComponent();

        Title = Loc.Get("PagePlaylistsTitle");
        AddLabel = Loc.Get("PlaylistAdd");
        EmptyText = Loc.Get("PlaylistEmpty");
        MenuPlay = Loc.Get("PlaylistMenuPlay");
        MenuRename = Loc.Get("PlaylistMenuRename");
        MenuDelete = Loc.Get("PlaylistMenuDelete");

        Playlists =
        [
            new PlaylistItem { Name = Loc.Get("PlaylistDemoFavorites") },
            new PlaylistItem { Name = Loc.Get("PlaylistDemoSleep") },
            new PlaylistItem { Name = Loc.Get("PlaylistDemoRoad") },
            new PlaylistItem { Name = Loc.Get("PlaylistDemoFocus") },
        ];

        Loaded += PlaylistsPage_Loaded;
    }

    public string Title { get; }
    public string AddLabel { get; }
    public string EmptyText { get; }
    public string MenuPlay { get; }
    public string MenuRename { get; }
    public string MenuDelete { get; }

    public ObservableCollection<PlaylistItem> Playlists { get; }

    private void PlaylistsPage_Loaded(object sender, RoutedEventArgs e) => UpdateEmptyState();

    private void UpdateEmptyState()
    {
        var empty = Playlists.Count == 0;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        PlaylistsGrid.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BtnAddPlaylist_Click(object sender, RoutedEventArgs e)
    {
        Playlists.Add(new PlaylistItem { Name = $"{Loc.Get("PlaylistNewPrefix")} {Playlists.Count + 1}" });
        UpdateEmptyState();
    }

    private void PlaylistsGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        // 打开歌单详情：阶段二
    }

    private void PlaylistsGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe && fe.DataContext is PlaylistItem item)
        {
            PlaylistsGrid.SelectedItem = item;
        }
    }

    private void MenuPlay_Click(object sender, RoutedEventArgs e)
    {
    }

    private void MenuRename_Click(object sender, RoutedEventArgs e)
    {
    }

    private void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        PlaylistItem? item = PlaylistsGrid.SelectedItem as PlaylistItem
            ?? (sender as FrameworkElement)?.DataContext as PlaylistItem;
        if (item is not null)
        {
            Playlists.Remove(item);
            UpdateEmptyState();
        }
    }
}
