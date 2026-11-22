using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Float_Hearing.Services;

/// <summary>
/// 基于 DesktopAcrylicController 的系统亚克力（Base / Thin / Default）。
/// XAML DesktopAcrylicBackdrop 本版本无 Kind，故用 Composition 控制器。
/// </summary>
internal sealed class AcrylicSystemBackdrop : SystemBackdrop, IDisposable
{
    private readonly DesktopAcrylicController _controller = new();
    private readonly SystemBackdropConfiguration _configuration = new();
    private ICompositionSupportsSystemBackdrop? _target;
    private Window? _window;
    private bool _disposed;

    public DesktopAcrylicKind Kind { get; set; } = DesktopAcrylicKind.Default;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(target, xamlRoot);
        _target = target;
        _controller.Kind = Kind;
        _configuration.IsInputActive = true;
        _controller.AddSystemBackdropTarget(target);
        _controller.SetSystemBackdropConfiguration(_configuration);
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
    {
        try
        {
            _controller.RemoveSystemBackdropTarget(target);
        }
        catch
        {
        }

        if (ReferenceEquals(_target, target))
        {
            _target = null;
        }

        base.OnTargetDisconnected(target);
    }

    public void Attach(Window window)
    {
        _window = window;
        window.Activated += Window_Activated;
        window.Closed += Window_Closed;
        // 若窗口已激活，先同步一次
        _configuration.IsInputActive = true;
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        _configuration.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    private void Window_Closed(object sender, WindowEventArgs args) => Dispose();

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

        try
        {
            if (_target is not null)
            {
                _controller.RemoveSystemBackdropTarget(_target);
                _target = null;
            }

            _controller.Dispose();
        }
        catch
        {
        }
    }
}

/// <summary>强类型 Mica Kind，避免反射失败导致 Alt 无效。</summary>
internal static class BackdropFactory
{
    public static SystemBackdrop CreateMica(bool alt) => new MicaBackdrop
    {
        // 注意：枚举是 Base / BaseAlt，不是 Alt
        Kind = alt ? MicaKind.BaseAlt : MicaKind.Base,
    };

    public static SystemBackdrop CreateAcrylic(DesktopAcrylicKind kind, Window window)
    {
        var backdrop = new AcrylicSystemBackdrop { Kind = kind };
        backdrop.Attach(window);
        return backdrop;
    }
}
