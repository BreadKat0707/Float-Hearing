using Float_Hearing.Services;
using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

/// <summary>应用内启动 Splash：短暂显示后由主窗口切入主内容。</summary>
public sealed partial class SplashPage : Page
{
    public SplashPage()
    {
        InitializeComponent();
        AppTitle = Loc.Get("AppTitle");
    }

    public string AppTitle { get; }
}
