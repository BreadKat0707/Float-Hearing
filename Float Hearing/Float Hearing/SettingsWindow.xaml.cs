using Float_Hearing.Pages;
using Float_Hearing.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing;

/// <summary>
/// 设置独立窗口：与主窗口一致的 Tall TitleBar（返回/导航折叠/图标）+ NavigationView。
/// 由主窗口标题栏「设置」按钮打开；返回可关闭窗口。
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
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AppWindow.SetIcon("AppIcon.ico");
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 720));
        }

        Title = title;
        WindowTracker.Register(this);

        // 与全局主题同步
        if (Content is FrameworkElement fe)
        {
            fe.RequestedTheme = App.Theme.Mode switch
            {
                AppThemeMode.Light => ElementTheme.Light,
                AppThemeMode.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }

        // 全局背景
        BackgroundService.Instance.ApplyTo(this);
        BackgroundService.Instance.BackgroundChanged += OnBackgroundChanged;
        Closed += (_, _) =>
        {
            BackgroundService.Instance.BackgroundChanged -= OnBackgroundChanged;
            WindowTracker.Unregister(this);
        };
    }

    private void OnBackgroundChanged(object? sender, EventArgs e)
        => BackgroundService.Instance.ApplyTo(this);

    private void SettingsTitleBar_BackRequested(TitleBar sender, object args)
    {
        if (SettingsFrame.CanGoBack)
        {
            SettingsFrame.GoBack();
        }
        else
        {
            // 设置窗无上级页面 → 关闭
            Close();
        }
    }

    private void SettingsTitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        SettingsNav.IsPaneOpen = !SettingsNav.IsPaneOpen;
    }

    private void SettingsNav_Loaded(object sender, RoutedEventArgs e)
    {
        if (SettingsFrame.Content is null)
        {
            SettingsFrame.Navigate(typeof(SettingsPage));
        }
    }
}
