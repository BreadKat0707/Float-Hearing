using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Float_Hearing.Services;

/// <summary>应用主题：跟随系统 / 浅色 / 深色。</summary>
public enum AppThemeMode
{
    System,
    Light,
    Dark,
}

/// <summary>
/// 三态主题切换。
/// 经 Dispatcher 延迟应用：避免在 RadioButtons.SelectionChanged 同步栈内改 RequestedTheme
/// （WinUI 下易触发 winrt::hresult_error / 布局重入）。
/// </summary>
public sealed class ThemeService
{
    public static ThemeService Instance { get; } = new();

    public event EventHandler<AppThemeMode>? ThemeChanged;

    private AppThemeMode _mode = AppThemeMode.System;
    private AppThemeMode? _pending;

    public AppThemeMode Mode => _mode;

    /// <summary>启动时注入已存主题（不触发事件、不 Apply；随后 Initialize 统一生效）。</summary>
    public void Preload(AppThemeMode mode)
    {
        _mode = mode;
        _pending = null;
    }

    public void Initialize()
    {
        ApplyNow(_mode);
    }

    public void SetMode(AppThemeMode mode)
    {
        if (_mode == mode && _pending is null)
        {
            return;
        }

        _mode = mode;
        _pending = mode;

        // 排队到 UI 线程下一拍，打断 SelectionChanged 调用栈
        var dq = DispatcherQueue.GetForCurrentThread();
        if (dq is not null && dq.TryEnqueue(ApplyPending))
        {
            // 已入队
        }
        else
        {
            ApplyPending();
        }

        SettingsService.SaveCurrent();
        ThemeChanged?.Invoke(this, mode);
    }

    private void ApplyPending()
    {
        if (_pending is not { } mode)
        {
            return;
        }

        _pending = null;
        ApplyNow(mode);
    }

    private void ApplyNow(AppThemeMode mode)
    {
        var elementTheme = ToElementTheme(mode);
        if (Application.Current is App app)
        {
            app.SetElementTheme(elementTheme);
        }
    }

    private static ElementTheme ToElementTheme(AppThemeMode mode) => mode switch
    {
        AppThemeMode.Light => ElementTheme.Light,
        AppThemeMode.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };
}
