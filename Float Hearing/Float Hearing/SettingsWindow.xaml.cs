using Float_Hearing.Pages;
using Float_Hearing.Services;
using Microsoft.UI.Xaml;

namespace Float_Hearing;

/// <summary>
/// 设置独立窗口：左侧 NavigationView 导航 + 右侧页框。
/// 由主窗口标题栏「设置」按钮打开。
/// </summary>
public sealed partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        var title = Loc.Get("NavSettings");
        SettingsTitleBar.Title = title;
        NavAppearance.Content = Loc.Get("SettingsAppearance");

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(SettingsTitleBar);

        if (AppWindow is not null)
        {
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
            AppWindow.SetIcon("AppIcon.ico");
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 720));
        }

        Title = title;
        WindowTracker.Register(this);
        Closed += (_, _) => WindowTracker.Unregister(this);

        // 与全局主题同步（打开时套用当前主题）
        if (Content is FrameworkElement fe)
        {
            fe.RequestedTheme = App.Theme.Mode switch
            {
                AppThemeMode.Light => Microsoft.UI.Xaml.ElementTheme.Light,
                AppThemeMode.Dark => Microsoft.UI.Xaml.ElementTheme.Dark,
                _ => Microsoft.UI.Xaml.ElementTheme.Default,
            };
        }
    }

    private void SettingsNav_Loaded(object sender, RoutedEventArgs e)
    {
        if (SettingsFrame.Content is null)
        {
            SettingsFrame.Navigate(typeof(SettingsPage));
        }
    }
}
