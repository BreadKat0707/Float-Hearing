using Float_Hearing.Dialogs;
using Float_Hearing.Models;
using Float_Hearing.Pages;
using Float_Hearing.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace Float_Hearing;

/// <summary>
/// 主窗口：内置 TitleBar + 分组侧栏（音乐库 / 听歌记录 / 歌单）+ 底部 PlayBar。
/// 歌单项：封面图标、拖拽排序、右键菜单；首项为新建歌单。
/// </summary>
public sealed partial class MainWindow : Window
{
    private const string PlaylistTagPrefix = "Playlist:";

    private SettingsWindow? _settingsWindow;
    private bool _navInitialized;
    private NavigationViewItem? _draggingPlaylistItem;

    public MainWindow()
    {
        InitializeComponent();

        AppTitle = Loc.Get("AppTitle");
        SearchPlaceholder = Loc.Get("SearchPlaceholder");
        NavGroupLibrary = Loc.Get("NavGroupLibrary");
        NavGroupHistory = Loc.Get("NavGroupHistory");
        NavGroupPlaylists = Loc.Get("NavGroupPlaylists");
        NavSongs = Loc.Get("NavSongs");
        NavAlbums = Loc.Get("NavAlbums");
        NavArtists = Loc.Get("NavArtists");
        NavGenres = Loc.Get("NavGenres");
        NavFolders = Loc.Get("NavFolders");
        NavRecent = Loc.Get("NavRecent");
        NavStats = Loc.Get("NavStats");
        NavIdeas = Loc.Get("NavIdeas");
        NavNewPlaylist = Loc.Get("PlaylistAdd");
        NavSleepTimer = Loc.Get("NavSleepTimer");
        NavSettings = Loc.Get("NavSettings");
        NavMusicLibrary = Loc.Get("MusicLibraryTitle");
        NavMusicRefresh = Loc.Get("MusicLibraryRefresh");
        MenuPlay = Loc.Get("PlaylistMenuPlay");
        MenuRename = Loc.Get("PlaylistMenuRename");
        MenuDelete = Loc.Get("PlaylistMenuDelete");

        HeaderLibrary.Content = NavGroupLibrary;
        HeaderHistory.Content = NavGroupHistory;
        HeaderPlaylists.Content = NavGroupPlaylists;

        // Toolkit TitleBar：AutoConfigure 会处理 ExtendsContentIntoTitleBar + SetTitleBar
        // 高度由 DisplayMode=Tall 控制；勿再设 PreferredHeightOption（与 AutoConfigure 冲突 → 0x8007139F）
        AppTitleBar.Window = this;

        if (AppWindow is not null)
        {
            AppWindow.SetIcon("AppIcon.ico");
        }

        Title = AppTitle;

        PlaylistStore.Items.CollectionChanged += (_, _) => RebuildPlaylistNavItems();
        BackgroundService.Instance.ApplyTo(this);
        BackgroundService.Instance.BackgroundChanged += OnBackgroundChanged;
        Closed += (_, _) =>
        {
            try
            {
                BackgroundService.Instance.BackgroundChanged -= OnBackgroundChanged;
                BackgroundService.Instance.OnWindowClosed(this);
            }
            catch (Exception)
            {
                // 关窗清理失败不阻断
            }
        };
    }

    private void OnBackgroundChanged(object? sender, EventArgs e)
        => BackgroundService.Instance.ApplyTo(this);

    public string AppTitle { get; }
    public string SearchPlaceholder { get; }
    public string NavGroupLibrary { get; }
    public string NavGroupHistory { get; }
    public string NavGroupPlaylists { get; }
    public string NavSongs { get; }
    public string NavAlbums { get; }
    public string NavArtists { get; }
    public string NavGenres { get; }
    public string NavFolders { get; }
    public string NavRecent { get; }
    public string NavStats { get; }
    public string NavIdeas { get; }
    public string NavNewPlaylist { get; }
    public string NavSleepTimer { get; }
    public string NavSettings { get; }
    public string NavMusicLibrary { get; }
    public string NavMusicRefresh { get; }
    public string MenuPlay { get; }
    public string MenuRename { get; }
    public string MenuDelete { get; }

    private void RootNav_Loaded(object sender, RoutedEventArgs e)
    {
        if (_navInitialized)
        {
            return;
        }

        _navInitialized = true;
        RebuildPlaylistNavItems();
        // 启动画面走 MSIX 清单 SplashScreen，不进应用内页
        ContentFrame.Navigate(typeof(SongsPage));
        UpdateBackButton();
    }

    #region TitleBar commands

    private void AppTitleBar_BackButtonClick(object sender, object args) => GoBack();

    private void AppTitleBar_PaneButtonClick(object sender, object args)
        => RootNav.IsPaneOpen = !RootNav.IsPaneOpen;

    private async void BtnMusicLibrary_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.XamlRoot is null)
        {
            return;
        }

        await MusicLibraryDialog.ShowAsync(ContentFrame.XamlRoot);
    }

    private void BtnRefreshLibrary_Click(object sender, RoutedEventArgs e)
    {
        // 刷新媒体库：扫描就绪后接 LibraryScanner
    }

    private async void BtnSleepTimer_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.XamlRoot is null)
        {
            return;
        }

        await SleepTimerDialog.ShowAsync(ContentFrame.XamlRoot);
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        _settingsWindow ??= new SettingsWindow();
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Activate();
        _settingsWindow.AppWindow?.MoveInZOrderAtTop();
    }

    #endregion

    #region Sidebar navigation

    private void RootNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        if (tag == "NewPlaylist")
        {
            var created = PlaylistStore.AddNew();
            // CollectionChanged 会重建侧栏；选中新建项
            RootNav.SelectedItem = FindPlaylistItem(created.Id);
            ContentFrame.Navigate(typeof(PlaylistDetailPage), created);
            return;
        }

        if (tag.StartsWith(PlaylistTagPrefix, StringComparison.Ordinal))
        {
            var id = tag[PlaylistTagPrefix.Length..];
            var playlist = PlaylistStore.Items.FirstOrDefault(p => p.Id == id);
            if (playlist is not null)
            {
                ContentFrame.Navigate(typeof(PlaylistDetailPage), playlist);
            }

            return;
        }

        var pageType = tag switch
        {
            "Songs" => typeof(SongsPage),
            "Albums" => typeof(AlbumsPage),
            "Artists" => typeof(ArtistsPage),
            "Genres" => typeof(GenresPage),
            "Folders" => typeof(FoldersPage),
            "Recent" => typeof(RecentPage),
            "Stats" => typeof(StatsPage),
            "Ideas" => typeof(IdeasPage),
            _ => null,
        };

        if (pageType is null || ContentFrame.CurrentSourcePageType == pageType)
        {
            return;
        }

        ContentFrame.Navigate(pageType);
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        UpdateBackButton();
        if (e.Parameter is PlaylistItem pl)
        {
            RootNav.SelectedItem = FindPlaylistItem(pl.Id);
        }
        else
        {
            SyncNavSelection(e.SourcePageType);
        }
    }

    private void SyncNavSelection(Type? pageType)
    {
        if (pageType is null)
        {
            return;
        }

        foreach (var raw in RootNav.MenuItems)
        {
            if (raw is NavigationViewItem item && MatchPage(item, pageType))
            {
                RootNav.SelectedItem = item;
                return;
            }
        }
    }

    private static bool MatchPage(NavigationViewItem item, Type pageType) => item.Tag switch
    {
        "Songs" => pageType == typeof(SongsPage),
        "Albums" => pageType == typeof(AlbumsPage),
        "Artists" => pageType == typeof(ArtistsPage),
        "Genres" => pageType == typeof(GenresPage),
        "Folders" => pageType == typeof(FoldersPage),
        "Recent" => pageType == typeof(RecentPage),
        "Stats" => pageType == typeof(StatsPage),
        "Ideas" => pageType == typeof(IdeasPage),
        _ => false,
    };

    private void GoBack()
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void UpdateBackButton() => AppTitleBar.IsBackButtonVisible = true;

    #endregion

    #region Playlist sidebar items (cover / drag / context menu)

    /// <summary>按 PlaylistStore 顺序重建「新建歌单」之后的侧栏歌单项。</summary>
    private void RebuildPlaylistNavItems()
    {
        // 移除旧的歌单导航项（保留 Header 与 NewPlaylist）
        for (var i = RootNav.MenuItems.Count - 1; i >= 0; i--)
        {
            if (RootNav.MenuItems[i] is NavigationViewItem nvi
                && nvi.Tag is string tag
                && tag.StartsWith(PlaylistTagPrefix, StringComparison.Ordinal))
            {
                RootNav.MenuItems.RemoveAt(i);
            }
        }

        var insertAt = RootNav.MenuItems.IndexOf(NavItemNewPlaylist) + 1;
        if (insertAt <= 0)
        {
            insertAt = RootNav.MenuItems.Count;
        }

        for (var i = 0; i < PlaylistStore.Items.Count; i++)
        {
            var playlist = PlaylistStore.Items[i];
            var navItem = CreatePlaylistNavItem(playlist);
            RootNav.MenuItems.Insert(insertAt + i, navItem);
        }
    }

    private NavigationViewItem CreatePlaylistNavItem(PlaylistItem playlist)
    {
        var item = new NavigationViewItem
        {
            Content = playlist.Name,
            Tag = PlaylistTagPrefix + playlist.Id,
        };

        // 封面代替图标；无封面则占位符
        if (!string.IsNullOrEmpty(playlist.CoverUri))
        {
            item.Icon = new ImageIcon
            {
                Source = new BitmapImage(new Uri(playlist.CoverUri, UriKind.Absolute)),
            };
        }
        else
        {
            item.Icon = new FontIcon { Glyph = "", FontSize = 16 };
        }

        // 右键菜单
        var flyout = new MenuFlyout();
        var play = new MenuFlyoutItem { Text = MenuPlay, Icon = new SymbolIcon(Symbol.Play) };
        var rename = new MenuFlyoutItem { Text = MenuRename, Icon = new SymbolIcon(Symbol.Edit) };
        var delete = new MenuFlyoutItem { Text = MenuDelete, Icon = new SymbolIcon(Symbol.Delete) };
        play.Click += (_, _) => { /* 接播放引擎 */ };
        rename.Click += (_, _) => { /* 阶段二：重命名对话框 */ };
        delete.Click += (_, _) => PlaylistStore.Items.Remove(playlist);
        flyout.Items.Add(play);
        flyout.Items.Add(rename);
        flyout.Items.Add(new MenuFlyoutSeparator());
        flyout.Items.Add(delete);
        item.ContextFlyout = flyout;

        // 拖拽排序
        item.CanDrag = true;
        item.DragStarting += PlaylistItem_DragStarting;
        item.DragOver += PlaylistItem_DragOver;
        item.Drop += PlaylistItem_Drop;

        return item;
    }

    private void PlaylistItem_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is not NavigationViewItem item || item.Tag is not string tag
            || !tag.StartsWith(PlaylistTagPrefix, StringComparison.Ordinal))
        {
            args.Cancel = true;
            return;
        }

        _draggingPlaylistItem = item;
        args.Data.RequestedOperation = DataPackageOperation.Move;
        args.Data.SetText(tag[PlaylistTagPrefix.Length..]);
    }

    private void PlaylistItem_DragOver(object sender, DragEventArgs args)
    {
        if (_draggingPlaylistItem is null || ReferenceEquals(sender, _draggingPlaylistItem))
        {
            args.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        args.AcceptedOperation = DataPackageOperation.Move;
        args.DragUIOverride.IsCaptionVisible = false;
        args.DragUIOverride.IsGlyphVisible = false;
    }

    private async void PlaylistItem_Drop(object sender, DragEventArgs args)
    {
        if (_draggingPlaylistItem is null
            || sender is not NavigationViewItem target
            || target.Tag is not string targetTag
            || !targetTag.StartsWith(PlaylistTagPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var def = args.GetDeferral();
        try
        {
            if (!args.DataView.Contains(StandardDataFormats.Text))
            {
                return;
            }

            var dragId = await args.DataView.GetTextAsync();
            var dragItem = PlaylistStore.Items.FirstOrDefault(p => p.Id == dragId);
            var targetItem = PlaylistStore.Items.FirstOrDefault(p => p.Id == targetTag[PlaylistTagPrefix.Length..]);
            if (dragItem is null || targetItem is null || dragItem == targetItem)
            {
                return;
            }

            var newIndex = PlaylistStore.IndexOf(targetItem);
            PlaylistStore.Move(dragItem, newIndex);
        }
        finally
        {
            _draggingPlaylistItem = null;
            def.Complete();
        }
    }

    private NavigationViewItem? FindPlaylistItem(string id)
    {
        var tag = PlaylistTagPrefix + id;
        foreach (var raw in RootNav.MenuItems)
        {
            if (raw is NavigationViewItem item && Equals(item.Tag, tag))
            {
                return item;
            }
        }

        return null;
    }

    #endregion
}
