using Float_Hearing.Models;
using Float_Hearing.Services;
using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

/// <summary>
/// 旧歌单网格页：侧栏已改为分组歌单项，此页暂不导航保留（后续可作批量管理视图）。
/// </summary>
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
        Loaded += (_, _) => BindStore();
    }

    public string Title { get; }
    public string AddLabel { get; }
    public string EmptyText { get; }
    public string MenuPlay { get; }
    public string MenuRename { get; }
    public string MenuDelete { get; }

    public System.Collections.ObjectModel.ObservableCollection<PlaylistItem> Playlists => PlaylistStore.Items;

    private void BindStore()
    {
        PlaylistsGrid.ItemsSource = Playlists;
        UpdateEmptyState();
        Playlists.CollectionChanged += (_, _) => UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        var empty = Playlists.Count == 0;
        EmptyState.Visibility = empty ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        PlaylistsGrid.Visibility = empty ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
    }

    private void BtnAddPlaylist_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PlaylistStore.AddNew();
        UpdateEmptyState();
    }

    private void PlaylistsGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
    }

    private void PlaylistsGrid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is Microsoft.UI.Xaml.FrameworkElement fe && fe.DataContext is PlaylistItem item)
        {
            PlaylistsGrid.SelectedItem = item;
        }
    }

    private void MenuPlay_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
    }

    private void MenuRename_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
    }

    private void MenuDelete_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PlaylistItem? item = PlaylistsGrid.SelectedItem as PlaylistItem
            ?? (sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext as PlaylistItem;
        if (item is not null)
        {
            PlaylistStore.Items.Remove(item);
            UpdateEmptyState();
        }
    }
}
