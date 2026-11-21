using Float_Hearing.Services;
using Microsoft.UI.Xaml;

namespace Float_Hearing;

/// <summary>
/// 应用入口：创建主窗口，初始化主题服务。
/// 服务以后经 App.* 静态入口访问（不引入 DI）。
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>主窗口（选择器 HWND 等从此取）。</summary>
    public static Window? MainWindow { get; private set; }

    public static ThemeService Theme { get; } = ThemeService.Instance;

    /// <summary>设置根元素主题（ThemeService 调用）。</summary>
    internal void SetElementTheme(ElementTheme theme)
    {
        if (_window?.Content is FrameworkElement fe)
        {
            fe.RequestedTheme = theme;
        }

        foreach (var w in WindowTracker.All)
        {
            if (w.Content is FrameworkElement we)
            {
                we.RequestedTheme = theme;
            }
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        MainWindow = _window;
        WindowTracker.Register(_window);
        _window.Activate();

        Theme.Initialize();
    }
}

/// <summary>跟踪已打开窗口，便于全局换主题。</summary>
internal static class WindowTracker
{
    private static readonly List<Window> _all = new();

    public static IReadOnlyList<Window> All => _all;

    public static void Register(Window window)
    {
        if (!_all.Contains(window))
        {
            _all.Add(window);
        }
    }
}
