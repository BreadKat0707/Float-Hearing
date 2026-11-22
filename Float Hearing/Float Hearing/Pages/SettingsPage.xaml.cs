using Float_Hearing.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

/// <summary>设置页：Community Toolkit SettingsCard / SettingsExpander + 三态主题。</summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Title = Loc.Get("PageSettingsTitle");
        AppearanceLabel = Loc.Get("SettingsAppearance");
        ThemeLabel = Loc.Get("SettingsTheme");
        ThemeDescription = Loc.Get("SettingsThemeDescription");
        ThemeSystemLabel = Loc.Get("SettingsThemeSystem");
        ThemeLightLabel = Loc.Get("SettingsThemeLight");
        ThemeDarkLabel = Loc.Get("SettingsThemeDark");
        AppearanceMoreLabel = Loc.Get("SettingsAppearanceMore");
        AppearanceMoreDescription = Loc.Get("SettingsAppearanceMoreDescription");
        MicaLabel = Loc.Get("SettingsMica");
        MicaDescription = Loc.Get("SettingsMicaDescription");
        PlayBarLabel = Loc.Get("SettingsPlayBar");
        PlayBarDescription = Loc.Get("SettingsPlayBarDescription");

        Loaded += SettingsPage_Loaded;
    }

    public string Title { get; }
    public string AppearanceLabel { get; }
    public string ThemeLabel { get; }
    public string ThemeDescription { get; }
    public string ThemeSystemLabel { get; }
    public string ThemeLightLabel { get; }
    public string ThemeDarkLabel { get; }
    public string AppearanceMoreLabel { get; }
    public string AppearanceMoreDescription { get; }
    public string MicaLabel { get; }
    public string MicaDescription { get; }
    public string PlayBarLabel { get; }
    public string PlayBarDescription { get; }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
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

    private void MicaToggle_Toggled(object sender, RoutedEventArgs e)
    {
        // 后续：切换 MainWindow SystemBackdrop（需用户确认后接线）
    }
}
