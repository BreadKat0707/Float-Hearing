using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class SleepTimerPage : Page
{
    private readonly PlaceholderContent _content = new("PageSleepTimerTitle");

    public SleepTimerPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}