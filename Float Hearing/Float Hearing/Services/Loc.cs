using Windows.ApplicationModel.Resources;

namespace Float_Hearing.Services;

/// <summary>
/// 本地化取词。新增 UI 文案时必须在 Strings/*/Resources.resw 同步补齐所有语言。
/// </summary>
public static class Loc
{
    private static ResourceLoader? _loader;

    private static ResourceLoader Loader => _loader ??= new ResourceLoader();

    /// <summary>按资源名取当前语言字符串；缺失时返回 key 本身便于发现漏翻。</summary>
    public static string Get(string key)
    {
        try
        {
            var value = Loader.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }
}
