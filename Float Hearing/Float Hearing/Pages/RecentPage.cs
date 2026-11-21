using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class RecentPage : Page
{
    private readonly PlaceholderContent _content = new("PageRecentTitle");

    public RecentPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}