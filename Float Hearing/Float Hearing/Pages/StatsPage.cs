using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class StatsPage : Page
{
    private readonly PlaceholderContent _content = new("PageStatsTitle");

    public StatsPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}