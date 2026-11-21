using Float_Hearing.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Dialogs;

/// <summary>计划暂停（睡眠定时）对话框：仅 UI，时长选择暂不接播放逻辑。</summary>
public static class SleepTimerDialog
{
    public static async Task ShowAsync(XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = Loc.Get("NavSleepTimer"),
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = Loc.Get("SleepTimerConfirm"),
            CloseButtonText = Loc.Get("SleepTimerCancel"),
        };

        var options = new RadioButtons
        {
            Header = Loc.Get("SleepTimerDurationLabel"),
        };
        foreach (var key in new[] { "SleepTimer15", "SleepTimer30", "SleepTimer60", "SleepTimerEndOfTrack" })
        {
            options.Items.Add(Loc.Get(key));
        }

        options.SelectedIndex = 1; // 默认 30 分钟
        dialog.Content = options;

        _ = await dialog.ShowAsync();
        // 选择结果暂不持久化（无播放引擎）
    }
}
