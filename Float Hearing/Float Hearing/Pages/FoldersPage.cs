using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

public sealed partial class FoldersPage : Page
{
    private readonly PlaceholderContent _content = new("PageFoldersTitle");

    public FoldersPage()
    {
        InitializeComponent();
    }

    public string Title => _content.Title;

    public string Subtitle => _content.Subtitle;
}