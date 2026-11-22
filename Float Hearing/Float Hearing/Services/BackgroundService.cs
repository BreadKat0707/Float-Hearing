using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Float_Hearing.Services;

/// <summary>窗口背景模式（全局设置）。</summary>
public enum BackgroundMode
{
    Solid,
    Mica,
    MicaAlt,
    AcrylicBase,
    AcrylicThin,
    AcrylicDefault,
    AcrylicCustom,
    /// <summary>流体背景：来自其它项目，此处仅占位。</summary>
    Fluid,
    Image,
}

/// <summary>
/// 全局背景：Solid / Mica / Mica Alt / Acrylic(Base·Thin·Default·Custom) / Fluid 占位 / 图片(dim+blur)。
/// 作用于 WindowTracker 中的窗口。
/// </summary>
public sealed class BackgroundService
{
    private readonly Dictionary<Window, (Image Image, Border Dim)> _imageLayers = [];

    public static BackgroundService Instance { get; } = new();

    public event EventHandler? BackgroundChanged;

    public BackgroundMode Mode { get; private set; } = BackgroundMode.Mica;

    public string SolidColor { get; set; } = "#202020";

    public string AcrylicTintColor { get; set; } = "#80FFFFFF";

    public double AcrylicTintOpacity { get; set; } = 0.5;

    public double AcrylicLuminosity { get; set; } = 0.5;

    public string? ImagePath { get; set; }

    public double ImageDim { get; set; } = 0.35;

    /// <summary>高斯模糊半径 0~50（当前实现：有图时叠加；完整 GPU 模糊后续增强）。</summary>
    public double ImageBlur { get; set; } = 0;

    public void Initialize() => ApplyAll();

    public void SetMode(BackgroundMode mode)
    {
        Mode = mode;
        ApplyAll();
        BackgroundChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NotifySettingsChanged()
    {
        ApplyAll();
        BackgroundChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyAll()
    {
        foreach (var w in WindowTracker.Snapshot())
        {
            ApplyTo(w);
        }
    }

    public void ApplyTo(Window window)
    {
        try
        {
            if (window.Content is not Grid root)
            {
                return;
            }

            window.SystemBackdrop = Mode switch
            {
                BackgroundMode.Mica => CreateMica(false),
                BackgroundMode.MicaAlt => CreateMica(true),
                BackgroundMode.AcrylicDefault => new DesktopAcrylicBackdrop(),
                _ => null,
            };

            var layers = GetOrCreateLayers(window, root);

            switch (Mode)
            {
                case BackgroundMode.Solid:
                    root.Background = ToSolid(SolidColor);
                    HideLayers(layers);
                    break;

                case BackgroundMode.AcrylicBase:
                    root.Background = CreateAcrylic("#CCFFFFFF", 0.85, 0.6);
                    HideLayers(layers);
                    break;

                case BackgroundMode.AcrylicThin:
                    root.Background = CreateAcrylic("#66FFFFFF", 0.30, 0.8);
                    HideLayers(layers);
                    break;

                case BackgroundMode.AcrylicCustom:
                    root.Background = CreateAcrylic(AcrylicTintColor, AcrylicTintOpacity, AcrylicLuminosity);
                    HideLayers(layers);
                    break;

                case BackgroundMode.Fluid:
                    root.Background = CreateFluidPlaceholder();
                    HideLayers(layers);
                    break;

                case BackgroundMode.Image:
                    ShowImage(window, root, layers);
                    break;

                default:
                    root.Background = null;
                    HideLayers(layers);
                    break;
            }
        }
        catch (Exception)
        {
            // 窗口已关闭
        }
    }

    private static MicaBackdrop CreateMica(bool alt)
    {
        // MicaBackdrop.Kind：Base / Alt（Mica Alt）
        var backdrop = new MicaBackdrop();
        try
        {
            var kindProp = typeof(MicaBackdrop).GetProperty("Kind");
            if (kindProp is not null)
            {
                var kindType = kindProp.PropertyType;
                var altValue = Enum.Parse(kindType, alt ? "Alt" : "Base");
                kindProp.SetValue(backdrop, altValue);
            }
        }
        catch
        {
            // 无 Kind 属性则默认 Base
        }

        return backdrop;
    }

    private (Image Image, Border Dim) GetOrCreateLayers(Window window, Grid root)
    {
        if (_imageLayers.TryGetValue(window, out var existing)
            && existing.Image is not null
            && existing.Image.Parent == root)
        {
            return existing;
        }

        var image = new Image
        {
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };
        var dim = new Border
        {
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };

        root.Children.Insert(0, dim);
        root.Children.Insert(0, image);
        var pair = (image, dim);
        _imageLayers[window] = pair;
        return pair;
    }

    private void ShowImage(Window window, Grid root, (Image Image, Border Dim) layers)
    {
        if (string.IsNullOrWhiteSpace(ImagePath) || !File.Exists(ImagePath))
        {
            root.Background = ToSolid(SolidColor);
            HideLayers(layers);
            return;
        }

        var bmp = new BitmapImage
        {
            CreateOptions = BitmapCreateOptions.IgnoreImageCache,
            UriSource = new Uri(Path.GetFullPath(ImagePath), UriKind.Absolute),
        };
        layers.Image.Source = bmp;
        layers.Image.Visibility = Visibility.Visible;
        layers.Dim.Visibility = Visibility.Visible;

        // 压暗 + 模糊半径映射为额外压暗（GPU 模糊待增强，避免无 Win2D 时崩溃）
        var blurFactor = Math.Clamp(ImageBlur / 50.0, 0, 1) * 0.15;
        var dimOpacity = Math.Clamp(ImageDim + blurFactor, 0, 1);
        layers.Dim.Background = new SolidColorBrush(Microsoft.UI.Colors.Black)
        {
            Opacity = (float)dimOpacity,
        };

        root.Background = null;
        _ = window;
    }

    private static void HideLayers((Image Image, Border Dim) layers)
    {
        layers.Image.Visibility = Visibility.Collapsed;
        layers.Image.Source = null;
        layers.Dim.Visibility = Visibility.Collapsed;
    }

    private static SolidColorBrush ToSolid(string hex)
        => new(ParseColor(hex.Length >= 7 ? hex : "#202020"));

    private static AcrylicBrush CreateAcrylic(string tint, double tintOpacity, double luminosity)
    {
        var brush = new AcrylicBrush
        {
            TintColor = ParseColor(tint),
            TintOpacity = Math.Clamp(tintOpacity, 0, 1),
            FallbackColor = ParseColor("#CC1C1C1C"),
        };

        try
        {
            var prop = typeof(AcrylicBrush).GetProperty("LuminosityOpacity");
            if (prop is { CanWrite: true })
            {
                prop.SetValue(brush, Math.Clamp(luminosity, 0, 1));
            }
        }
        catch
        {
        }

        return brush;
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        try
        {
            hex = hex.Trim();
            if (hex.Length is 7 or 9 && hex[0] == '#')
            {
                byte a = 255;
                var offset = 1;
                if (hex.Length == 9)
                {
                    a = Convert.ToByte(hex.Substring(1, 2), 16);
                    offset = 3;
                }

                var r = Convert.ToByte(hex.Substring(offset, 2), 16);
                var g = Convert.ToByte(hex.Substring(offset + 2, 2), 16);
                var b = Convert.ToByte(hex.Substring(offset + 4, 2), 16);
                return Microsoft.UI.ColorHelper.FromArgb(a, r, g, b);
            }
        }
        catch
        {
        }

        return Microsoft.UI.Colors.Black;
    }

    private static Brush CreateFluidPlaceholder()
    {
        var g = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1),
        };
        g.GradientStops.Add(new GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(255, 36, 56, 88),
            Offset = 0,
        });
        g.GradientStops.Add(new GradientStop
        {
            Color = Microsoft.UI.ColorHelper.FromArgb(255, 88, 40, 76),
            Offset = 1,
        });
        return g;
    }
}
