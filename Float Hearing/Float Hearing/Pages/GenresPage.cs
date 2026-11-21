using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class GenresPage : Page
{
    private readonly PlaceholderContent _content = new("PageGenresTitle");

    public GenresPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}