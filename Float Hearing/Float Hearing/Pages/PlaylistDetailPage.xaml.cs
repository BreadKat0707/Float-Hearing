using System.ComponentModel;
using Float_Hearing.Models;
using Float_Hearing.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Float_Hearing.Pages;

/// <summary>歌单详情占位页（侧栏点选歌单后打开）。</summary>
public sealed partial class PlaylistDetailPage : Page, INotifyPropertyChanged
{
    private string _playlistName = string.Empty;

    public PlaylistDetailPage()
    {
        InitializeComponent();
        Subtitle = Loc.Get("PlaylistDetailSubtitle");
        Hint = Loc.Get("PlaylistDetailHint");
    }

    public string PlaylistName
    {
        get => _playlistName;
        private set
        {
            _playlistName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaylistName)));
        }
    }

    public string Subtitle { get; }

    public string Hint { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        PlaylistName = e.Parameter is PlaylistItem item
            ? item.Name
            : Loc.Get("PagePlaylistsTitle");
    }
}
