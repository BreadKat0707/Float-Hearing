using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Float_Hearing.Services;

/// <summary>
/// 基于 DesktopAcrylicController 的系统亚克力（Base / Thin / Default）。
/// 关闭/切换时避免 Dispose 与 OnTargetDisconnected 双重 RemoveSystemBackdropTarget。
/// </summary>
internal sealed class AcrylicSystemBackdrop : SystemBackdrop, IDisposable
{
    private readonly DesktopAcrylicController _controller = new();
    private readonly SystemBackdropConfiguration _configuration = new();
    private ICompositionSupportsSystemBackdrop? _target;
    private Window? _window;
    private bool _targetAttached;
    private bool _disposed;

    public DesktopAcrylicKind Kind { get; set; } = DesktopAcrylicKind.Default;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(target, xamlRoot);
        if (_disposed)
        {
            return;
        }

        _target = target;
        _controller.Kind = Kind;
        _configuration.IsInputActive = true;
        _controller.AddSystemBackdropTarget(target);
        _controller.SetSystemBackdropConfiguration(_configuration);
        _targetAttached = true;
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
    {
        // 仅在此路径移除一次；Dispose 不再重复 Remove
        if (_targetAttached && !_disposed)
        {
            try
            {
                _controller.RemoveSystemBackdropTarget(target);
            }
            catch (Exception)
            {
                // 已移除/目标已失效
            }

            _targetAttached = false;
            _target = null;
        }

        try
        {
            base.OnTargetDisconnected(target);
        }
        catch (Exception)
        {
        }
    }

    public void Attach(Window window)
    {
        _window = window;
        window.Activated += Window_Activated;
        window.Closed += Window_Closed;
        _configuration.IsInputActive = true;
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_disposed)
        {
            return;
        }

        _configuration.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // 先摘掉 SystemBackdrop，让框架先走 OnTargetDisconnected，再 Dispose 控制器
        try
        {
            if (_window?.SystemBackdrop == this)
            {
                _window.SystemBackdrop = null;
            }
        }
        catch (Exception)
        {
        }

        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_window is not null)
        {
            _window.Activated -= Window_Activated;
            _window.Closed -= Window_Closed;
            _window = null;
        }

        // 若仍挂着 target（框架尚未 Disconnect），此处 Remove；否则交给 OnTargetDisconnected
        if (_targetAttached && _target is not null)
        {
            try
            {
                _controller.RemoveSystemBackdropTarget(_target);
            }
            catch (Exception)
            {
            }

            _targetAttached = false;
            _target = null;
        }

        try
        {
            _controller.Dispose();
        }
        catch (Exception)
        {
        }
    }
}

/// <summary>强类型 Mica Kind（Base / BaseAlt），避免反射失败。</summary>
internal static class BackdropFactory
{
    public static SystemBackdrop CreateMica(bool alt) => new MicaBackdrop
    {
        // 枚举是 Base / BaseAlt，不是 Alt
        Kind = alt ? MicaKind.BaseAlt : MicaKind.Base,
    };

    public static AcrylicSystemBackdrop CreateAcrylic(DesktopAcrylicKind kind, Window window)
    {
        var backdrop = new AcrylicSystemBackdrop { Kind = kind };
        backdrop.Attach(window);
        return backdrop;
    }
}
