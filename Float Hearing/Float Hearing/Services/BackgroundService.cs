using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
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
/// Mica=强类型 MicaKind；Acrylic Base/Thin/Default=DesktopAcrylicController；
/// Custom=DesktopAcrylic 模糊材质 + 半透明着色层（AcrylicBrush 采不到桌面，不用）。
/// </summary>
public sealed class BackgroundService
{
    private readonly Dictionary<Window, ImageLayer> _layers = [];
    private readonly Dictionary<Window, AcrylicSystemBackdrop> _acrylicBackdrops = [];
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

            // 释放上一轮自定义亚克力控制器
            DisposeAcrylic(window);

            window.SystemBackdrop = Mode switch
            {
                BackgroundMode.Mica => BackdropFactory.CreateMica(false),
                BackgroundMode.MicaAlt => BackdropFactory.CreateMica(true),
                BackgroundMode.AcrylicBase => CreateAndTrackAcrylic(window, DesktopAcrylicKind.Base),
                BackgroundMode.AcrylicThin => CreateAndTrackAcrylic(window, DesktopAcrylicKind.Thin),
                BackgroundMode.AcrylicDefault => CreateAndTrackAcrylic(window, DesktopAcrylicKind.Default),
                BackgroundMode.AcrylicCustom => CreateAndTrackAcrylic(window, DesktopAcrylicKind.Default),
                _ => null,
            };

            var layer = GetOrCreateLayer(window, root);

            switch (Mode)
            {
                case BackgroundMode.Solid:
                    root.Background = ToSolid(SolidColor);
                    HideLayer(layer);
                    break;

                case BackgroundMode.AcrylicCustom:
                    // 桌面亚克力模糊来自 SystemBackdrop；此处叠半透明 tint（alpha=着色不透明度）
                    root.Background = CreateTintOverlay();
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
                    // Mica / Acrylic* → SystemBackdrop 提供材质，根透明
                    root.Background = null;
                    HideLayer(layer);
                    break;
            }
        }
        catch (Exception)
        {
        }
    }

    private SystemBackdrop CreateAndTrackAcrylic(Window window, DesktopAcrylicKind kind)
    {
        var backdrop = new AcrylicSystemBackdrop { Kind = kind };
        backdrop.Attach(window);
        _acrylicBackdrops[window] = backdrop;
        return backdrop;
    }

    private void DisposeAcrylic(Window window)
    {
        if (_acrylicBackdrops.Remove(window, out var old))
        {
            old.Dispose();
        }
    }

    /// <summary>自定义亚克力着色层（配合 DesktopAcrylic 模糊）。</summary>
    private Brush CreateTintOverlay()
    {
        var color = ParseColor(AcrylicTintColor);
        var alpha = (byte)Math.Clamp(AcrylicTintOpacity * (byte.MaxValue), 0, 255);
        // 若 hex 已带 alpha 通道则优先用其 alpha * 不透明度滑条
        var a = color.A == 255 ? alpha : (byte)(color.A * AcrylicTintOpacity);
        var c = Microsoft.UI.ColorHelper.FromArgb(a, color.R, color.G, color.B);
        return new SolidColorBrush(c);
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
