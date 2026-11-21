using Float_Hearing.Pages;
using Float_Hearing.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Float_Hearing;

/// <summary>
/// 主窗口：Tall 自定义标题栏 + NavigationView 侧栏 + 底部 PlayBar。
/// 返回 / 导航折叠在标题栏；IsBackButtonVisible / IsPaneToggleButtonVisible 由标题栏按钮承担。
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AppTitle = Loc.Get("AppTitle");
        SearchPlaceholder = Loc.Get("SearchPlaceholder");
        NavSongs = Loc.Get("NavSongs");
        NavAlbums = Loc.Get("NavAlbums");
        NavArtists = Loc.Get("NavArtists");
        NavGenres = Loc.Get("NavGenres");
        NavFolders = Loc.Get("NavFolders");
        NavPlaylists = Loc.Get("NavPlaylists");
        NavRecent = Loc.Get("NavRecent");
        NavStats = Loc.Get("NavStats");
        NavIdeas = Loc.Get("NavIdeas");
        NavSleepTimer = Loc.Get("NavSleepTimer");
        NavSettings = Loc.Get("NavSettings");

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (AppWindow is not null)
        {
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AppWindow.SetIcon("AppIcon.ico");
        }

        Title = AppTitle;
    }

    public string AppTitle { get; }
    public string SearchPlaceholder { get; }
    public string NavSongs { get; }
    public string NavAlbums { get; }
    public string NavArtists { get; }
    public string NavGenres { get; }
    public string NavFolders { get; }
    public string NavPlaylists { get; }
    public string NavRecent { get; }
    public string NavStats { get; }
    public string NavIdeas { get; }
    public string NavSleepTimer { get; }
    public string NavSettings { get; }

    private bool _navInitialized;

    private void RootNav_Loaded(object sender, RoutedEventArgs e)
    {
        if (_navInitialized)
        {
            return;
        }

        _navInitialized = true;
        // 默认歌曲页
        ContentFrame.Navigate(typeof(SongsPage));
        UpdateBackButton();
    }

    private void RootNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        var pageType = tag switch
        {
            "Songs" => typeof(SongsPage),
            "Albums" => typeof(AlbumsPage),
            "Artists" => typeof(ArtistsPage),
            "Genres" => typeof(GenresPage),
            "Folders" => typeof(FoldersPage),
            "Playlists" => typeof(PlaylistsPage),
            "Recent" => typeof(RecentPage),
            "Stats" => typeof(StatsPage),
            "Ideas" => typeof(IdeasPage),
            "SleepTimer" => typeof(SleepTimerPage),
            "Settings" => typeof(SettingsPage),
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
        SyncNavSelection(e.SourcePageType);
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

        foreach (var raw in RootNav.FooterMenuItems)
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
        "Playlists" => pageType == typeof(PlaylistsPage),
        "Recent" => pageType == typeof(RecentPage),
        "Stats" => pageType == typeof(StatsPage),
        "Ideas" => pageType == typeof(IdeasPage),
        "SleepTimer" => pageType == typeof(SleepTimerPage),
        "Settings" => pageType == typeof(SettingsPage),
        _ => false,
    };

    private void BtnBack_Click(object sender, RoutedEventArgs e) => GoBack();

    private void GoBack()
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void BtnTogglePane_Click(object sender, RoutedEventArgs e)
    {
        RootNav.IsPaneOpen = !RootNav.IsPaneOpen;
    }

    private void UpdateBackButton()
    {
        BtnBack.IsEnabled = ContentFrame.CanGoBack;
        BtnBack.Opacity = ContentFrame.CanGoBack ? 1.0 : 0.4;
    }

    /// <summary>标题栏高度同步（Tall）。</summary>
    private void SyncTitleBarHeight()
    {
        if (AppWindow is null)
        {
            return;
        }

        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppTitleBar.Height = 48;
    }
}
