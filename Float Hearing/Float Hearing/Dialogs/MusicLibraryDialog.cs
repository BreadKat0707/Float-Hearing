using Float_Hearing.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Dialogs;

/// <summary>音乐库（扫描文件夹）对话框：占位 UI，后续接 FolderPicker 与扫描。</summary>
public static class MusicLibraryDialog
{
    public static async Task ShowAsync(XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = Loc.Get("MusicLibraryTitle"),
            PrimaryButtonText = Loc.Get("MusicLibraryPickFolder"),
            CloseButtonText = Loc.Get("SleepTimerCancel"),
            DefaultButton = ContentDialogButton.Close,
        };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = Loc.Get("MusicLibraryDescription"),
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
        });
        panel.Children.Add(new TextBlock
        {
            Text = Loc.Get("MusicLibraryComingSoon"),
            Opacity = 0.7,
            Style = (Microsoft.UI.Xaml.Style)App.Current.Resources["CaptionTextBlockStyle"],
        });
        dialog.Content = panel;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // TODO：FolderPicker 选择目录并触发扫描（阶段二）
        }
    }
}
