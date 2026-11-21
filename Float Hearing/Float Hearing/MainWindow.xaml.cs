using Float_Hearing.Dialogs;
using Float_Hearing.Pages;
using Float_Hearing.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Float_Hearing;

/// <summary>
/// 主窗口：内置 TitleBar（返回/折叠系统样式）+ NavigationView 侧栏 + 底部 PlayBar。
/// 计划暂停=ContentDialog；设置=独立 SettingsWindow。
/// </summary>
public sealed partial class MainWindow : Window
{
    private SettingsWindow? _settingsWindow;

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
        NavMusicLibrary = Loc.Get("MusicLibraryTitle");

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
    public string NavMusicLibrary { get; }

    private bool _navInitialized;

    private void RootNav_Loaded(object sender, RoutedEventArgs e)
    {
        if (_navInitialized)
        {
            return;
        }

        _navInitialized = true;
        ContentFrame.Navigate(typeof(SongsPage));
        UpdateBackButton();
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        GoBack();
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        RootNav.IsPaneOpen = !RootNav.IsPaneOpen;
    }

    private async void BtnMusicLibrary_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.XamlRoot is null)
        {
            return;
        }

        await MusicLibraryDialog.ShowAsync(ContentFrame.XamlRoot);
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
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Activate();
        _settingsWindow.AppWindow?.MoveInZOrderAtTop();
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
        _ => false,
    };

    private void GoBack()
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void UpdateBackButton()
    {
        // TitleBar 内置返回按钮可用性：无历史时禁用
        AppTitleBar.IsBackButtonVisible = true;
        // TitleBar 不直接暴露 IsEnabled 给 back；无历史时仍显示但点击无操作（与 Gallery 一致）
    }
}
