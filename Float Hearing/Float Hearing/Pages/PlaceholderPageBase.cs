using Float_Hearing.Services;

namespace Float_Hearing.Pages;

/// <summary>占位页文案（标题 + 即将实现），本地化取词。页面本身仍继承 Page。</summary>
public sealed class PlaceholderContent
{
    public PlaceholderContent(string titleKey)
    {
        Title = Loc.Get(titleKey);
        Subtitle = Loc.Get("PlaceholderComingSoon");
    }

    public string Title { get; }

    public string Subtitle { get; }
}
