using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Float_Hearing.Services;

/// <summary>
/// 基于 DesktopAcrylicController 的系统亚克力（Base / Thin / Default）。
/// 关窗：只退订事件，不 Remove/Dispose（组合对象随窗口销毁，避免 0xc000 / winrt::hresult_error）。
/// 切换背景：由 BackgroundService.DisposeAcrylic 先摘 SystemBackdrop 再清理。
/// </summary>
internal sealed class AcrylicSystemBackdrop : SystemBackdrop, IDisposable
{
    private readonly DesktopAcrylicController _controller = new();
    private readonly SystemBackdropConfiguration _configuration = new();
    private ICompositionSupportsSystemBackdrop? _target;
    private Window? _window;
    private bool _targetAttached;
    private bool _disposed;
    private bool _closed;

    public DesktopAcrylicKind Kind { get; set; } = DesktopAcrylicKind.Default;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(target, xamlRoot);
        if (_disposed || _closed)
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
        // 关窗路径：目标已失效，跳过 Remove，避免 hresult_error
        if (_closed)
        {
            _targetAttached = false;
            _target = null;
            try
            {
                base.OnTargetDisconnected(target);
            }
            catch (Exception)
            {
            }

            return;
        }

        // 切换背景：单次 Remove
        if (_targetAttached && !_disposed)
        {
            try
            {
                _controller.RemoveSystemBackdropTarget(target);
            }
            catch (Exception)
            {
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
        if (_disposed || _closed)
        {
            return;
        }

        try
        {
            _configuration.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
        catch (Exception)
        {
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // 关窗：只标记 + 退订。不改 SystemBackdrop、不 Remove、不 Dispose 控制器。
        if (_closed)
        {
            return;
        }

        _closed = true;

        if (_window is not null)
        {
            try
            {
                _window.Activated -= Window_Activated;
                _window.Closed -= Window_Closed;
            }
            catch (Exception)
            {
            }

            _window = null;
        }

        _targetAttached = false;
        _target = null;
    }

    /// <summary>仅在切换背景、窗口仍存活时调用。</summary>
    public void Dispose()
    {
        if (_disposed || _closed)
        {
            return;
        }

        _disposed = true;

        if (_window is not null)
        {
            try
            {
                _window.Activated -= Window_Activated;
                _window.Closed -= Window_Closed;
            }
            catch (Exception)
            {
            }

            _window = null;
        }

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

/// <summary>强类型 Mica Kind（Base / BaseAlt）。</summary>
internal static class BackdropFactory
{
    public static SystemBackdrop CreateMica(bool alt) => new MicaBackdrop
    {
        Kind = alt ? MicaKind.BaseAlt : MicaKind.Base,
    };

    public static AcrylicSystemBackdrop CreateAcrylic(DesktopAcrylicKind kind, Window window)
    {
        var backdrop = new AcrylicSystemBackdrop { Kind = kind };
        backdrop.Attach(window);
        return backdrop;
    }
}
