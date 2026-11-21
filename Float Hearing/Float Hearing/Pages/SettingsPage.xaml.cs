using Float_Hearing.Services;
using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

/// <summary>设置页：三态主题切换（本页是壳阶段唯一有真实行为的设置）。</summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Title = Loc.Get("PageSettingsTitle");
        AppearanceLabel = Loc.Get("SettingsAppearance");
        ThemeLabel = Loc.Get("SettingsTheme");
        ThemeSystemLabel = Loc.Get("SettingsThemeSystem");
        ThemeLightLabel = Loc.Get("SettingsThemeLight");
        ThemeDarkLabel = Loc.Get("SettingsThemeDark");

        Loaded += SettingsPage_Loaded;
    }

    public string Title { get; }
    public string AppearanceLabel { get; }
    public string ThemeLabel { get; }
    public string ThemeSystemLabel { get; }
    public string ThemeLightLabel { get; }
    public string ThemeDarkLabel { get; }

    private void SettingsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // 避免 Loaded 时触发 SelectionChanged 写回
        ThemeSelector.SelectionChanged -= ThemeSelector_SelectionChanged;
        ThemeSelector.SelectedIndex = App.Theme.Mode switch
        {
            AppThemeMode.Light => 1,
            AppThemeMode.Dark => 2,
            _ => 0,
        };
        ThemeSelector.SelectionChanged += ThemeSelector_SelectionChanged;
    }

    private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeSelector.SelectedIndex < 0)
        {
            return;
        }

        var mode = ThemeSelector.SelectedIndex switch
        {
            1 => AppThemeMode.Light,
            2 => AppThemeMode.Dark,
            _ => AppThemeMode.System,
        };
        App.Theme.SetMode(mode);
    }
}
