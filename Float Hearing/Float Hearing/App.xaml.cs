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
        UnhandledException += OnUnhandledException;
    }

    /// <summary>主窗口（选择器 HWND 等从此取）。</summary>
    public static Window? MainWindow { get; private set; }

    public static ThemeService Theme { get; } = ThemeService.Instance;

    /// <summary>设置根元素主题（ThemeService 调用）。逐窗口隔离，避免一窗失败拖垮全局。</summary>
    internal void SetElementTheme(ElementTheme theme)
    {
        static void ApplyTheme(Window window, ElementTheme t)
        {
            try
            {
                if (window.Content is FrameworkElement fe)
                {
                    fe.RequestedTheme = t;
                }
            }
            catch (Exception)
            {
                // 窗口已关闭/句柄失效时忽略
            }
        }

        if (_window is not null)
        {
            ApplyTheme(_window, theme);
        }

        foreach (var w in WindowTracker.Snapshot())
        {
            ApplyTheme(w, theme);
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FloatHearing", "startup-error.log");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.AppendAllText(path,
                $"{DateTime.Now:O}\n{e.Exception}\n---\n");
        }
        catch
        {
            // 日志失败不影响退出路径
        }

        e.Handled = true;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        MainWindow = _window;
        WindowTracker.Register(_window);
        _window.Activate();

        // 先灌入已存设置，再初始化（避免默认值覆盖磁盘）
        SettingsService.ApplyToServices(SettingsService.Load());

        Theme.Initialize();
        BackgroundService.Instance.Initialize();
    }
}

/// <summary>跟踪已打开窗口，便于全局换主题；关闭时注销，避免访问已销毁窗口。</summary>
internal static class WindowTracker
{
    private static readonly object _gate = new();
    private static readonly List<Window> _all = new();

    public static void Register(Window window)
    {
        lock (_gate)
        {
            if (!_all.Contains(window))
            {
                _all.Add(window);
            }
        }

        // 失焦回退纯色：激活状态交 BackgroundService
        window.Activated += (_, e) =>
        {
            var active = e.WindowActivationState != WindowActivationState.Deactivated;
            BackgroundService.Instance.NotifyWindowActivation(window, active);
        };
        window.Closed += (_, _) =>
        {
            BackgroundService.Instance.OnWindowClosed(window);
            Unregister(window);
        };
    }

    public static void Unregister(Window window)
    {
        lock (_gate)
        {
            _all.Remove(window);
        }
    }

    /// <summary>拷贝快照，避免遍历中修改集合。</summary>
    public static Window[] Snapshot()
    {
        lock (_gate)
        {
            return _all.ToArray();
        }
    }
}
