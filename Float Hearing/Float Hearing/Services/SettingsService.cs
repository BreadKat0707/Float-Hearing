using System.Text.Json;

namespace Float_Hearing.Services;

/// <summary>持久化设置（主题 + 背景）。JSON 落在 %LocalAppData%\FloatHearing\settings.json。</summary>
public sealed class AppSettings
{
    public string ThemeMode { get; set; } = "System";

    public string BackgroundMode { get; set; } = "Mica";

    public string SolidColor { get; set; } = "#202020";

    public string AcrylicTintColor { get; set; } = "#80FFFFFF";

    public double AcrylicTintOpacity { get; set; } = 0.5;

    public double AcrylicLuminosity { get; set; } = 0.5;

    public string? ImagePath { get; set; }

    public double ImageDim { get; set; } = 0.35;

    public double ImageBlur { get; set; } = 0;
}

/// <summary>设置读写：启动 Load→灌入服务；各变更点 SaveCurrent。</summary>
public static class SettingsService
{
    private static readonly string FilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FloatHearing", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            // 损坏/无权限 → 用默认
        }

        return new AppSettings();
    }

    /// <summary>从 ThemeService + BackgroundService 当前值落盘。</summary>
    public static void SaveCurrent()
    {
        try
        {
            var bg = BackgroundService.Instance;
            var settings = new AppSettings
            {
                ThemeMode = ThemeService.Instance.Mode.ToString(),
                BackgroundMode = bg.Mode.ToString(),
                SolidColor = bg.SolidColor,
                AcrylicTintColor = bg.AcrylicTintColor,
                AcrylicTintOpacity = bg.AcrylicTintOpacity,
                AcrylicLuminosity = bg.AcrylicLuminosity,
                ImagePath = bg.ImagePath,
                ImageDim = bg.ImageDim,
                ImageBlur = bg.ImageBlur,
            };

            var dir = System.IO.Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch
        {
            // 写失败不影响运行
        }
    }

    /// <summary>启动时把已存设置灌进 Theme/Background（须在各自 Initialize 之前）。</summary>
    public static void ApplyToServices(AppSettings settings)
    {
        if (Enum.TryParse<AppThemeMode>(settings.ThemeMode, ignoreCase: true, out var theme))
        {
            ThemeService.Instance.Preload(theme);
        }

        var bg = BackgroundService.Instance;
        bg.SolidColor = settings.SolidColor;
        bg.AcrylicTintColor = settings.AcrylicTintColor;
        bg.AcrylicTintOpacity = settings.AcrylicTintOpacity;
        bg.AcrylicLuminosity = settings.AcrylicLuminosity;
        bg.ImagePath = settings.ImagePath;
        bg.ImageDim = settings.ImageDim;
        bg.ImageBlur = settings.ImageBlur;

        if (Enum.TryParse<BackgroundMode>(settings.BackgroundMode, ignoreCase: true, out var mode))
        {
            bg.PreloadMode(mode);
        }
    }
}
