using Float_Hearing.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Controls;

/// <summary>窗口底部播放条骨架：仅 UI 与本地化，不接播放引擎。</summary>
public sealed partial class PlayBar : UserControl
{
    public PlayBar()
    {
        InitializeComponent();
        TrackTitle = Loc.Get("PlayBarNothingPlaying");
        TrackArtist = Loc.Get("PlayBarUnknownArtist");
        PreviousLabel = Loc.Get("PlayPrevious");
        NextLabel = Loc.Get("PlayNext");
        PlayPauseLabel = Loc.Get("PlayPause");
    }

    public string TrackTitle { get; }
    public string TrackArtist { get; }
    public string PreviousLabel { get; }
    public string NextLabel { get; }
    public string PlayPauseLabel { get; }

    private void BtnPrevious_Click(object sender, RoutedEventArgs e)
    {
        // 阶段一：无播放引擎
    }

    private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        // 阶段一：无播放引擎
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        // 阶段一：无播放引擎
    }
}
