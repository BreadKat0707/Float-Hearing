using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class PlaylistsPage : Page
{
    private readonly PlaceholderContent _content = new("PagePlaylistsTitle");

    public PlaylistsPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}