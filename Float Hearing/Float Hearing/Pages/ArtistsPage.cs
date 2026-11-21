using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class ArtistsPage : Page
{
    private readonly PlaceholderContent _content = new("PageArtistsTitle");

    public ArtistsPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}