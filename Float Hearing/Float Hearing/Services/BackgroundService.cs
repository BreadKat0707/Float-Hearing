using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Numerics;
using Windows.Foundation;
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
    private DispatcherQueueTimer? _saveDebounce;

    public static BackgroundService Instance { get; } = new();

    public event EventHandler? BackgroundChanged;

    public BackgroundMode Mode { get; private set; } = BackgroundMode.Mica;

    public string SolidColor { get; set; } = "#202020";

    public string AcrylicTintColor { get; set; } = "#FFFFFF";

    public double AcrylicTintOpacity { get; set; } = 0.5;

    public double AcrylicLuminosity { get; set; } = 0.5;

    public string? ImagePath { get; set; }

    public double ImageDim { get; set; } = 0.35;

    /// <summary>高斯模糊半径 0~50（Win2D）。</summary>
    public double ImageBlur { get; set; } = 0;

    public void Initialize() => ApplyAll();

    /// <summary>启动时注入已存模式与参数（不 Apply；随后 Initialize 统一生效）。</summary>
    public void PreloadMode(BackgroundMode mode) => Mode = mode;

    public void SetMode(BackgroundMode mode)
    {
        Mode = mode;
        ApplyAll();
        SettingsService.SaveCurrent();
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
        DebounceSave();
    }

    /// <summary>滑条连拖时合并落盘，避免每 tick 写 JSON 卡 UI。</summary>
    private void DebounceSave()
    {
        var dq = DispatcherQueue.GetForCurrentThread();
        if (dq is null)
        {
            SettingsService.SaveCurrent();
            return;
        }

        if (_saveDebounce is null)
        {
            _saveDebounce = dq.CreateTimer();
            _saveDebounce.Interval = TimeSpan.FromMilliseconds(400);
            _saveDebounce.Tick += (_, _) =>
            {
                _saveDebounce.Stop();
                SettingsService.SaveCurrent();
            };
        }

        _saveDebounce.Stop();
        _saveDebounce.Start();
    }

    public void NotifySettingsChanged()
    {
        ApplyAll();
        SettingsService.SaveCurrent();
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
        if (!_acrylicBackdrops.Remove(window, out var old))
        {
            return;
        }

        // 窗口仍存活时：先断开再 Dispose（切换背景路径）
        try
        {
            if (ReferenceEquals(window.SystemBackdrop, old))
            {
                window.SystemBackdrop = null;
            }
        }
        catch (Exception)
        {
        }

        try
        {
            old.Dispose();
        }
        catch (Exception)
        {
        }
    }

    /// <summary>窗口关闭：从跟踪移除，不触碰组合对象（避免关窗 hresult_error）。</summary>
    public void OnWindowClosed(Window window)
    {
        _acrylicBackdrops.Remove(window);
        _layers.Remove(window);
    }

    /// <summary>自定义亚克力着色层（配合 DesktopAcrylic 模糊）。透明度只来自滑条，hex 仅取 RGB。</summary>
    private Brush CreateTintOverlay()
    {
        var color = ParseColor(AcrylicTintColor);
        var alpha = (byte)Math.Clamp(AcrylicTintOpacity * byte.MaxValue, 0, 255);
        var c = Microsoft.UI.ColorHelper.FromArgb(alpha, color.R, color.G, color.B);
        c = ApplyLuminosity(c, AcrylicLuminosity);
        return new SolidColorBrush(c);
    }

    /// <summary>
    /// Luminosity 滑条（0~1，0.5=不变）：把 tint 的 HSL 亮度向滑条值拉动，
    /// 白/灰 tint 也能看出变暗/变亮（纯白抬高仍封顶 255）。
    /// </summary>
    private static Windows.UI.Color ApplyLuminosity(Windows.UI.Color c, double luminosity)
    {
        var lum = Math.Clamp(luminosity, 0, 1);
        RgbToHsl(c.R, c.G, c.B, out var h, out var s, out var l);
        var newL = l + (lum - l) * 0.65;
        var (r, g, b) = HslToRgb(h, s, newL);
        return Microsoft.UI.ColorHelper.FromArgb(c.A, r, g, b);
    }

    private static void RgbToHsl(byte r0, byte g0, byte b0, out double h, out double s, out double l)
    {
        double r = r0 / 255.0, g = g0 / 255.0, b = b0 / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        l = (max + min) / 2;
        if (Math.Abs(max - min) < 1e-9)
        {
            h = 0;
            s = 0;
            return;
        }

        var d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        if (Math.Abs(max - r) < 1e-9)
        {
            h = (g - b) / d + (g < b ? 6 : 0);
        }
        else if (Math.Abs(max - g) < 1e-9)
        {
            h = (b - r) / d + 2;
        }
        else
        {
            h = (r - g) / d + 4;
        }

        h /= 6;
    }

    private static (byte R, byte G, byte B) HslToRgb(double h, double s, double l)
    {
        if (s < 1e-9)
        {
            var v = (byte)Math.Clamp(Math.Round(l * 255), 0, 255);
            return (v, v, v);
        }

        static double Hue2Rgb(double p, double q, double t)
        {
            if (t < 0)
            {
                t += 1;
            }

            if (t > 1)
            {
                t -= 1;
            }

            if (t < 1.0 / 6)
            {
                return p + (q - p) * 6 * t;
            }

            if (t < 1.0 / 2)
            {
                return q;
            }

            if (t < 2.0 / 3)
            {
                return p + (q - p) * (2.0 / 3 - t) * 6;
            }

            return p;
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        var r = Hue2Rgb(p, q, h + 1.0 / 3);
        var g = Hue2Rgb(p, q, h);
        var b = Hue2Rgb(p, q, h - 1.0 / 3);
        return (
            (byte)Math.Clamp(Math.Round(r * 255), 0, 255),
            (byte)Math.Clamp(Math.Round(g * 255), 0, 255),
            (byte)Math.Clamp(Math.Round(b * 255), 0, 255));
    }

    private sealed class ImageLayer
    {
        public Window Window { get; init; } = null!;
        public Image Image { get; init; } = null!;
        /// <summary>模糊专用宿主：子视觉挂在它上面；Image 仅在无模糊时可见。</summary>
        public Border BlurHost { get; init; } = null!;
        public Border Dim { get; init; } = null!;
        public string? LoadedPath { get; set; }

        public SpriteVisual? BlurVisual { get; set; }

        /// <summary>表面只按路径加载一次；拖半径不重载。</summary>
        public LoadedImageSurface? Surface { get; set; }

        public CompositionSurfaceBrush? SurfaceBrush { get; set; }

        public string? SurfacePath { get; set; }

        public CompositionEffectBrush? EffectBrush { get; set; }

        public double AppliedBlurRadius { get; set; } = -1;

        /// <summary>异步加载代数：换图/清层时丢弃过期 LoadCompleted。</summary>
        public int SurfaceGeneration { get; set; }

        public bool SurfaceReady => SurfaceBrush is not null;
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
        var blurHost = new Border
        {
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        var dim = new Border
        {
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };

        // 顺序：image → blurHost → dim → 其它内容
        root.Children.Insert(0, dim);
        root.Children.Insert(0, blurHost);
        root.Children.Insert(0, image);
        var layer = new ImageLayer { Window = window, Image = image, BlurHost = blurHost, Dim = dim };
        _layers[window] = layer;

        // 窗口缩放时同步模糊视觉尺寸
        blurHost.SizeChanged += (_, _) => SyncBlurSpriteSize(layer);
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

            // 换图 → 旧表面作废
            if (!string.Equals(layer.SurfacePath, path, StringComparison.OrdinalIgnoreCase))
            {
                InvalidateSurface(layer);
            }
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

    /// <summary>
    /// 高斯模糊（Win2D + Composition）：
    /// 表面按路径只加载一次；拖半径只重建 effect brush（不重载位图、不重建 surface）。
    /// 子视觉挂 BlurHost（保持 Visible），避免挂在 Collapsed 的 Image 上不渲染。
    /// </summary>
    private void ApplyBlur(Window window, ImageLayer layer, double radius)
    {
        radius = Math.Clamp(radius, 0, 50);
        var image = layer.Image;
        var host = layer.BlurHost;

        // 无模糊：恢复 Image，清掉宿主子视觉
        if (radius < 0.5)
        {
            ClearBlurVisual(layer);
            image.Visibility = Visibility.Visible;
            host.Visibility = Visibility.Collapsed;
            layer.AppliedBlurRadius = radius;
            return;
        }

        if (image.Source is not BitmapImage bmp || bmp.UriSource is null)
        {
            image.Visibility = Visibility.Visible;
            host.Visibility = Visibility.Collapsed;
            return;
        }

        // 表面未就绪：先保持原图可见，LoadCompleted 后会再进来
        if (!layer.SurfaceReady || layer.SurfacePath is null
            || !string.Equals(layer.SurfacePath, bmp.UriSource.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            EnsureSurfaceLoaded(layer, bmp.UriSource);
            image.Visibility = Visibility.Visible;
            host.Visibility = Visibility.Collapsed;
            return;
        }

        // 半径未变且视觉已在 → 只可能 dim 变了
        if (layer.BlurVisual is not null && layer.EffectBrush is not null
            && Math.Abs(layer.AppliedBlurRadius - radius) < 0.01)
        {
            image.Visibility = Visibility.Collapsed;
            host.Visibility = Visibility.Visible;
            SyncBlurSpriteSize(layer);
            return;
        }

        try
        {
            var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;

            // 只重建 effect brush；source 仍是缓存的 SurfaceBrush
            var effect = new GaussianBlurEffect
            {
                Name = "imageBlur",
                BlurAmount = (float)radius,
                BorderMode = EffectBorderMode.Hard,
                Source = new CompositionEffectSourceParameter("source"),
            };
            var factory = compositor.CreateEffectFactory(effect);
            var effectBrush = factory.CreateBrush();
            effectBrush.SetSourceParameter("source", layer.SurfaceBrush!);
            layer.EffectBrush = effectBrush;
            layer.AppliedBlurRadius = radius;

            if (layer.BlurVisual is null)
            {
                var sprite = compositor.CreateSpriteVisual();
                sprite.Brush = effectBrush;
                layer.BlurVisual = sprite;
                ElementCompositionPreview.SetElementChildVisual(host, sprite);
            }
            else
            {
                layer.BlurVisual.Brush = effectBrush;
            }

            // 先显示宿主再取尺寸（Image 可能已 Collapsed）
            host.Visibility = Visibility.Visible;
            image.Visibility = Visibility.Collapsed;
            SyncBlurSpriteSize(layer);
        }
        catch
        {
            ClearBlurVisual(layer);
            image.Visibility = Visibility.Visible;
            host.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>按路径加载表面一次；完成后再走 ApplyBlur 上视觉。</summary>
    private void EnsureSurfaceLoaded(ImageLayer layer, Uri uri)
    {
        var uriText = uri.ToString();
        if (layer.Surface is not null && layer.SurfacePath == uriText && layer.SurfaceBrush is not null)
        {
            return;
        }

        // 同一 URI 正在加载
        if (layer.Surface is not null && layer.SurfacePath == uriText && layer.SurfaceBrush is null)
        {
            return;
        }

        InvalidateSurface(layer);
        layer.SurfacePath = uriText;
        var generation = ++layer.SurfaceGeneration;

        try
        {
            var surface = LoadedImageSurface.StartLoadFromUri(uri);
            layer.Surface = surface;
            surface.LoadCompleted += (_, args) =>
            {
                try
                {
                    if (generation != layer.SurfaceGeneration)
                    {
                        return;
                    }

                    if (args.Status != LoadedImageSourceLoadStatus.Success)
                    {
                        layer.Surface = null;
                        layer.SurfacePath = null;
                        return;
                    }

                    var compositor = ElementCompositionPreview.GetElementVisual(layer.BlurHost).Compositor;
                    var surfaceBrush = compositor.CreateSurfaceBrush(surface);
                    surfaceBrush.Stretch = CompositionStretch.UniformToFill;
                    layer.SurfaceBrush = surfaceBrush;

                    // 表面就绪 → 立即按当前半径上模糊（回到所属窗口）
                    ApplyBlur(layer.Window, layer, ImageBlur);
                }
                catch
                {
                    layer.SurfaceBrush = null;
                }
            };
        }
        catch
        {
            layer.Surface = null;
            layer.SurfacePath = null;
        }
    }

    private static void InvalidateSurface(ImageLayer layer)
    {
        layer.SurfaceGeneration++;
        ClearBlurVisual(layer);
        layer.SurfaceBrush = null;
        layer.Surface = null;
        layer.SurfacePath = null;
        layer.EffectBrush = null;
        layer.AppliedBlurRadius = -1;
    }

    /// <summary>子视觉尺寸贴宿主（或 Image 实测尺寸兜底）。</summary>
    private static void SyncBlurSpriteSize(ImageLayer layer)
    {
        if (layer.BlurVisual is null)
        {
            return;
        }

        var host = layer.BlurHost;
        var image = layer.Image;
        var w = host.ActualWidth > 1 ? host.ActualWidth : image.ActualWidth > 1 ? image.ActualWidth : 1200;
        var h = host.ActualHeight > 1 ? host.ActualHeight : image.ActualHeight > 1 ? image.ActualHeight : 800;
        layer.BlurVisual.Size = new Vector2((float)w, (float)h);
    }

    private static void ClearBlurVisual(ImageLayer layer)
    {
        if (layer.BlurVisual is not null)
        {
            ElementCompositionPreview.SetElementChildVisual(layer.BlurHost, null);
            layer.BlurVisual = null;
        }

        layer.EffectBrush = null;
        layer.AppliedBlurRadius = -1;
    }

    private static void HideLayer(ImageLayer layer)
    {
        layer.Image.Visibility = Visibility.Collapsed;
        layer.Image.Source = null;
        layer.LoadedPath = null;
        layer.Dim.Visibility = Visibility.Collapsed;
        InvalidateSurface(layer);
        layer.BlurHost.Visibility = Visibility.Collapsed;
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
