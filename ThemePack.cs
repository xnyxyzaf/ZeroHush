namespace PixelStudio;

/// <summary>
/// 外部主题包。放在 exe 同目录的 Themes 文件夹下。
/// </summary>
public class ThemePack
{
    /// <summary>主题唯一 ID（比如 "ocean"、"sakura"）</summary>
    public string Id { get; set; } = "";

    /// <summary>主题显示名（比如 "海洋"）</summary>
    public string Name { get; set; } = "";

    /// <summary>窗口背景色，格式 "#RRGGBB"</summary>
    public string WindowBg { get; set; } = "";

    /// <summary>面板背景色，格式 "#RRGGBB"</summary>
    public string PanelBg { get; set; } = "";

    /// <summary>强调色，格式 "#RRGGBB"</summary>
    public string Accent { get; set; } = "";

    /// <summary>作者（可选）</summary>
    public string Author { get; set; } = "";

    /// <summary>版本（可选）</summary>
    public string Version { get; set; } = "";
}