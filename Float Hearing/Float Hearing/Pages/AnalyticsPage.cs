using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class AnalyticsPage : Page
{
    private readonly PlaceholderContent _content = new("PageAnalyticsTitle");

    public AnalyticsPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}