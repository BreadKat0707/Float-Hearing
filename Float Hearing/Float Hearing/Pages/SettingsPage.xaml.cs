using Float_Hearing.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Storage.Pickers;

namespace Float_Hearing.Pages;

/// <summary>设置页：SettingsCard/Expander + 主题 + 全局背景（含 Acrylic/图片参数）。</summary>
public sealed partial class SettingsPage : Page
{
    private bool _syncing;

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
        PlayBarLabel = Loc.Get("SettingsPlayBar");
        PlayBarDescription = Loc.Get("SettingsPlayBarDescription");

        BackgroundLabel = Loc.Get("BgSection");
        BackgroundDescription = Loc.Get("BgSectionDescription");
        BackgroundModeLabel = Loc.Get("BgMode");
        BgSolid = Loc.Get("BgSolid");
        BgMica = Loc.Get("BgMica");
        BgMicaAlt = Loc.Get("BgMicaAlt");
        BgAcrylicBase = Loc.Get("BgAcrylicBase");
        BgAcrylicThin = Loc.Get("BgAcrylicThin");
        BgAcrylicDefault = Loc.Get("BgAcrylicDefault");
        BgAcrylicCustom = Loc.Get("BgAcrylicCustom");
        BgFluid = Loc.Get("BgFluid");
        BgImage = Loc.Get("BgImage");
        BgSolidColor = Loc.Get("BgSolidColor");
        BgAcrylicTint = Loc.Get("BgAcrylicTint");
        BgAcrylicTintOpacity = Loc.Get("BgAcrylicTintOpacity");
        BgAcrylicLuminosity = Loc.Get("BgAcrylicLuminosity");
        BgImagePath = Loc.Get("BgImagePath");
        BgPickImage = Loc.Get("BgPickImage");
        BgImageDim = Loc.Get("BgImageDim");
        BgImageBlur = Loc.Get("BgImageBlur");

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
    public string PlayBarLabel { get; }
    public string PlayBarDescription { get; }
    public string BackgroundLabel { get; }
    public string BackgroundDescription { get; }
    public string BackgroundModeLabel { get; }
    public string BgSolid { get; }
    public string BgMica { get; }
    public string BgMicaAlt { get; }
    public string BgAcrylicBase { get; }
    public string BgAcrylicThin { get; }
    public string BgAcrylicDefault { get; }
    public string BgAcrylicCustom { get; }
    public string BgFluid { get; }
    public string BgImage { get; }
    public string BgSolidColor { get; }
    public string BgAcrylicTint { get; }
    public string BgAcrylicTintOpacity { get; }
    public string BgAcrylicLuminosity { get; }
    public string BgImagePath { get; }
    public string BgPickImage { get; }
    public string BgImageDim { get; }
    public string BgImageBlur { get; }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _syncing = true;
        try
        {
            ThemeSelector.SelectionChanged -= ThemeSelector_SelectionChanged;
            ThemeSelector.SelectedIndex = App.Theme.Mode switch
            {
                AppThemeMode.Light => 1,
                AppThemeMode.Dark => 2,
                _ => 0,
            };
            ThemeSelector.SelectionChanged += ThemeSelector_SelectionChanged;

            var bg = BackgroundService.Instance;
            BackgroundModeSelector.SelectedIndex = bg.Mode switch
            {
                BackgroundMode.Solid => 0,
                BackgroundMode.Mica => 1,
                BackgroundMode.MicaAlt => 2,
                BackgroundMode.AcrylicBase => 3,
                BackgroundMode.AcrylicThin => 4,
                BackgroundMode.AcrylicDefault => 5,
                BackgroundMode.AcrylicCustom => 6,
                BackgroundMode.Fluid => 7,
                BackgroundMode.Image => 8,
                _ => 1,
            };
            SolidColorBox.Text = bg.SolidColor;
            AcrylicTintBox.Text = bg.AcrylicTintColor;
            AcrylicOpacitySlider.Value = bg.AcrylicTintOpacity;
            AcrylicLumSlider.Value = bg.AcrylicLuminosity;
            ImageDimSlider.Value = bg.ImageDim;
            ImageBlurSlider.Value = bg.ImageBlur;
            ImagePathText.Text = bg.ImagePath ?? string.Empty;
            UpdateBackgroundCardVisibility(bg.Mode);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || ThemeSelector.SelectedIndex < 0)
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

    private void BackgroundModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || BackgroundModeSelector.SelectedItem is not ComboBoxItem { Tag: string tag })
        {
            return;
        }

        var mode = tag switch
        {
            "Solid" => BackgroundMode.Solid,
            "Mica" => BackgroundMode.Mica,
            "MicaAlt" => BackgroundMode.MicaAlt,
            "AcrylicBase" => BackgroundMode.AcrylicBase,
            "AcrylicThin" => BackgroundMode.AcrylicThin,
            "AcrylicDefault" => BackgroundMode.AcrylicDefault,
            "AcrylicCustom" => BackgroundMode.AcrylicCustom,
            "Fluid" => BackgroundMode.Fluid,
            "Image" => BackgroundMode.Image,
            _ => BackgroundMode.Mica,
        };

        BackgroundService.Instance.SetMode(mode);
        UpdateBackgroundCardVisibility(mode);
    }

    private void UpdateBackgroundCardVisibility(BackgroundMode mode)
    {
        CardSolidColor.Visibility = mode == BackgroundMode.Solid ? Visibility.Visible : Visibility.Collapsed;
        var acrylicCustom = mode == BackgroundMode.AcrylicCustom;
        CardAcrylicTint.Visibility = acrylicCustom ? Visibility.Visible : Visibility.Collapsed;
        CardAcrylicOpacity.Visibility = acrylicCustom ? Visibility.Visible : Visibility.Collapsed;
        CardAcrylicLum.Visibility = acrylicCustom ? Visibility.Visible : Visibility.Collapsed;
        var image = mode == BackgroundMode.Image;
        CardImagePath.Visibility = image ? Visibility.Visible : Visibility.Collapsed;
        CardImageDim.Visibility = image ? Visibility.Visible : Visibility.Collapsed;
        CardImageBlur.Visibility = image ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SolidColorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        BackgroundService.Instance.SolidColor = SolidColorBox.Text;
        if (BackgroundService.Instance.Mode == BackgroundMode.Solid)
        {
            BackgroundService.Instance.NotifySettingsChanged();
        }
        else
        {
            SettingsService.SaveCurrent();
        }
    }

    private void AcrylicTintBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        BackgroundService.Instance.AcrylicTintColor = AcrylicTintBox.Text;
        if (BackgroundService.Instance.Mode == BackgroundMode.AcrylicCustom)
        {
            BackgroundService.Instance.NotifySettingsChanged();
        }
        else
        {
            SettingsService.SaveCurrent();
        }
    }

    private void AcrylicOpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        BackgroundService.Instance.AcrylicTintOpacity = e.NewValue;
        if (BackgroundService.Instance.Mode == BackgroundMode.AcrylicCustom)
        {
            BackgroundService.Instance.NotifySettingsChanged();
        }
        else
        {
            SettingsService.SaveCurrent();
        }
    }

    private void AcrylicLumSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        BackgroundService.Instance.AcrylicLuminosity = e.NewValue;
        if (BackgroundService.Instance.Mode == BackgroundMode.AcrylicCustom)
        {
            BackgroundService.Instance.NotifySettingsChanged();
        }
        else
        {
            SettingsService.SaveCurrent();
        }
    }

    private void ImageDimSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        BackgroundService.Instance.ImageDim = e.NewValue;
        // 只刷 dim/blur，不重载位图
        BackgroundService.Instance.UpdateImageEffects();
    }

    private void ImageBlurSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        BackgroundService.Instance.ImageBlur = e.NewValue;
        BackgroundService.Instance.UpdateImageEffects();
    }

    private async void PickImage_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".webp");

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        BackgroundService.Instance.ImagePath = file.Path;
        ImagePathText.Text = file.Path;
        if (BackgroundService.Instance.Mode != BackgroundMode.Image)
        {
            BackgroundModeSelector.SelectedIndex = 8;
            BackgroundService.Instance.SetMode(BackgroundMode.Image);
            UpdateBackgroundCardVisibility(BackgroundMode.Image);
        }
        else
        {
            BackgroundService.Instance.NotifySettingsChanged();
        }
    }
}
