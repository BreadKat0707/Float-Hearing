using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Numerics;
using Windows.Graphics.Effects;

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
/// 亚克力 Base/Thin/Default 走 DesktopAcrylicBackdrop.Kind；Custom 走 AcrylicBrush 参数。
/// 图片：缓存位图，dim 仅改透明度；blur 用 Win2D GaussianBlurEffect。
/// </summary>
public sealed class BackgroundService
{
    private readonly Dictionary<Window, ImageLayer> _layers = [];
    private readonly Dictionary<string, BitmapImage> _bitmapCache = new(StringComparer.OrdinalIgnoreCase);

    public static BackgroundService Instance { get; } = new();

    public event EventHandler? BackgroundChanged;

    public BackgroundMode Mode { get; private set; } = BackgroundMode.Mica;

    public string SolidColor { get; set; } = "#202020";

    public string AcrylicTintColor { get; set; } = "#80FFFFFF";

    public double AcrylicTintOpacity { get; set; } = 0.5;

    public double AcrylicLuminosity { get; set; } = 0.5;

    public string? ImagePath { get; set; }

    public double ImageDim { get; set; } = 0.35;

    /// <summary>高斯模糊半径 0~50（Win2D）。</summary>
    public double ImageBlur { get; set; } = 0;

    public void Initialize() => ApplyAll();

    public void SetMode(BackgroundMode mode)
    {
        Mode = mode;
        ApplyAll();
        BackgroundChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>仅刷新图片压暗/模糊（拖动滑条时调用，避免重载位图）。</summary>
    public void UpdateImageEffects()
    {
        if (Mode != BackgroundMode.Image)
        {
            return;
        }

        foreach (var w in WindowTracker.Snapshot())
        {
            try
            {
                if (_layers.TryGetValue(w, out var layer) && layer.Image.Source is not null)
                {
                    ApplyDimAndBlur(w, layer);
                }
            }
            catch
            {
            }
        }

        BackgroundChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NotifySettingsChanged() => ApplyAll();

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
                BackgroundMode.AcrylicBase => CreateAcrylicBackdrop("Base"),
                BackgroundMode.AcrylicThin => CreateAcrylicBackdrop("Thin"),
                BackgroundMode.AcrylicDefault => CreateAcrylicBackdrop("Default"),
                _ => null,
            };

            var layer = GetOrCreateLayer(window, root);

            switch (Mode)
            {
                case BackgroundMode.Solid:
                    root.Background = ToSolid(SolidColor);
                    HideLayer(layer);
                    break;

                // Custom：AcrylicBrush（tint/opacity/luminosity 可调）；Base/Thin/Default 用 SystemBackdrop
                case BackgroundMode.AcrylicCustom:
                    root.Background = CreateAcrylicBrush(AcrylicTintColor, AcrylicTintOpacity, AcrylicLuminosity);
                    HideLayer(layer);
                    break;

                case BackgroundMode.Fluid:
                    root.Background = CreateFluidPlaceholder();
                    HideLayer(layer);
                    break;

                case BackgroundMode.Image:
                    ShowImage(window, root, layer);
                    break;

                default:
                    root.Background = null;
                    HideLayer(layer);
                    break;
            }
        }
        catch (Exception)
        {
        }
    }

    private static MicaBackdrop CreateMica(bool alt)
    {
        var backdrop = new MicaBackdrop();
        SetKind(backdrop, alt ? "Alt" : "Base");
        return backdrop;
    }

    private static DesktopAcrylicBackdrop CreateAcrylicBackdrop(string kind)
    {
        var backdrop = new DesktopAcrylicBackdrop();
        SetKind(backdrop, kind);
        return backdrop;
    }

    private static void SetKind(object backdrop, string kindName)
    {
        try
        {
            var prop = backdrop.GetType().GetProperty("Kind");
            if (prop is { CanWrite: true })
            {
                prop.SetValue(backdrop, Enum.Parse(prop.PropertyType, kindName));
            }
        }
        catch
        {
            // 无 Kind 则保持默认
        }
    }

    private sealed class ImageLayer
    {
        public Image Image { get; init; } = null!;
        public Border Dim { get; init; } = null!;
        public string? LoadedPath { get; set; }
        public SpriteVisual? BlurVisual { get; set; }
    }

    private ImageLayer GetOrCreateLayer(Window window, Grid root)
    {
        if (_layers.TryGetValue(window, out var existing) && existing.Image.Parent == root)
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
        var layer = new ImageLayer { Image = image, Dim = dim };
        _layers[window] = layer;
        return layer;
    }

    private void ShowImage(Window window, Grid root, ImageLayer layer)
    {
        if (string.IsNullOrWhiteSpace(ImagePath) || !File.Exists(ImagePath))
        {
            root.Background = ToSolid(SolidColor);
            HideLayer(layer);
            return;
        }

        var path = Path.GetFullPath(ImagePath);

        // 缓存位图：路径未变则不重载（dim 拖动不卡）
        if (layer.LoadedPath != path || layer.Image.Source is null)
        {
            BitmapImage bmp;
            if (_bitmapCache.TryGetValue(path, out var cached))
            {
                bmp = cached;
            }
            else
            {
                bmp = new BitmapImage();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                _bitmapCache[path] = bmp;
            }

            layer.Image.Source = bmp;
            layer.LoadedPath = path;
        }

        layer.Image.Visibility = Visibility.Visible;
        layer.Dim.Visibility = Visibility.Visible;
        root.Background = null;
        ApplyDimAndBlur(window, layer);
    }

    private void ApplyDimAndBlur(Window window, ImageLayer layer)
    {
        var dimOpacity = Math.Clamp(ImageDim, 0, 1);
        layer.Dim.Background = new SolidColorBrush(Microsoft.UI.Colors.Black)
        {
            Opacity = (float)dimOpacity,
        };

        ApplyBlur(window, layer, ImageBlur);
    }

    /// <summary>Win2D 高斯模糊：对 Image 建 SpriteVisual + GaussianBlurEffect。</summary>
    private static void ApplyBlur(Window window, ImageLayer layer, double radius)
    {
        radius = Math.Clamp(radius, 0, 50);
        var image = layer.Image;

        // 清除旧模糊 visual
        if (layer.BlurVisual is not null)
        {
            ElementCompositionPreview.SetElementChildVisual(image, null);
            layer.BlurVisual = null;
        }

        if (radius < 0.5 || image.Source is not BitmapImage bmp || bmp.UriSource is null)
        {
            // 无模糊时恢复 Image 自身绘制
            image.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(image);
            var compositor = visual.Compositor;

            var effect = new GaussianBlurEffect
            {
                Name = "imageBlur",
                BlurAmount = (float)radius,
                BorderMode = EffectBorderMode.Hard,
                Source = new CompositionEffectSourceParameter("source"),
            };

            var factory = compositor.CreateEffectFactory(effect);
            var effectBrush = factory.CreateBrush();

            var surface = LoadedImageSurface.StartLoadFromUri(bmp.UriSource);
            surface.LoadCompleted += (_, _) =>
            {
                try
                {
                    var surfaceBrush = compositor.CreateSurfaceBrush(surface);
                    surfaceBrush.Stretch = CompositionStretch.UniformToFill;
                    effectBrush.SetSourceParameter("source", surfaceBrush);

                    var sprite = compositor.CreateSpriteVisual();
                    var w = (float)(image.ActualWidth > 1 ? image.ActualWidth : 1200);
                    var h = (float)(image.ActualHeight > 1 ? image.ActualHeight : 800);
                    sprite.Size = new Vector2(w, h);
                    sprite.Brush = effectBrush;

                    // 模糊时隐藏原生 Image 绘制，只显示 SpriteVisual
                    image.Visibility = Visibility.Collapsed;
                    ElementCompositionPreview.SetElementChildVisual(image, sprite);
                    layer.BlurVisual = sprite;
                }
                catch
                {
                    image.Visibility = Visibility.Visible;
                }
            };
        }
        catch
        {
            image.Visibility = Visibility.Visible;
        }
    }

    private static void HideLayer(ImageLayer layer)
    {
        layer.Image.Visibility = Visibility.Collapsed;
        layer.Image.Source = null;
        layer.LoadedPath = null;
        layer.Dim.Visibility = Visibility.Collapsed;
        if (layer.BlurVisual is not null)
        {
            ElementCompositionPreview.SetElementChildVisual(layer.Image, null);
            layer.BlurVisual = null;
        }
    }

    private static SolidColorBrush ToSolid(string hex)
        => new(ParseColor(hex.Length >= 7 ? hex : "#202020"));

    /// <summary>自定义亚克力：AcrylicBrush（tint / tintOpacity / luminosity）。</summary>
    private static AcrylicBrush CreateAcrylicBrush(string tint, double tintOpacity, double luminosity)
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
