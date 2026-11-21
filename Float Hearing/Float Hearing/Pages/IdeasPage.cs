using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class IdeasPage : Page
{
    private readonly PlaceholderContent _content = new("PageIdeasTitle");

    public IdeasPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}