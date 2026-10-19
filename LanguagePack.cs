using System.Collections.Generic;

namespace PixelStudio;

/// <summary>
/// 外部语言包的数据模型。
/// 一个 JSON 文件代表一种语言。
/// </summary>
public class LanguagePack
{
    /// <summary>语言代码，如 "ja-JP"、"fr-FR"</summary>
    public string Code { get; set; } = "";

    /// <summary>语言显示名，如 "日本語"、"Français"</summary>
    public string Name { get; set; } = "";

    /// <summary>贡献者（可选）</summary>
    public string Author { get; set; } = "";

    /// <summary>语言包版本（可选）</summary>
    public string Version { get; set; } = "";

    /// <summary>所有翻译。key 对应 LanguageManager 里的 key</summary>
    public Dictionary<string, string> Strings { get; set; } = new();
}