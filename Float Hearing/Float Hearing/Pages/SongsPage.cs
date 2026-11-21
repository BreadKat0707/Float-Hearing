using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class SongsPage : Page
{
    private readonly PlaceholderContent _content = new("PageSongsTitle");

    public SongsPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}