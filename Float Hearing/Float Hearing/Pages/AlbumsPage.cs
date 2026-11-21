using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class AlbumsPage : Page
{
    private readonly PlaceholderContent _content = new("PageAlbumsTitle");

    public AlbumsPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}