using Microsoft.UI.Xaml;

namespace Float_Hearing.Services;

/// <summary>应用主题：跟随系统 / 浅色 / 深色。</summary>
public enum AppThemeMode
{
    System,
    Light,
    Dark,
}

/// <summary>三态主题切换。经 App.SetElementTheme 应用到已打开窗口的内容根。</summary>
public sealed class ThemeService
{
    public static ThemeService Instance { get; } = new();

    public event EventHandler<AppThemeMode>? ThemeChanged;

    private AppThemeMode _mode = AppThemeMode.System;

    public AppThemeMode Mode => _mode;

    public void Initialize()
    {
        Apply(AppThemeMode.System);
    }

    public void SetMode(AppThemeMode mode)
    {
        if (_mode == mode)
        {
            return;
        }

        _mode = mode;
        Apply(mode);
        ThemeChanged?.Invoke(this, mode);
    }

    private void Apply(AppThemeMode mode)
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
