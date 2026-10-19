using System;
using System.IO;
using System.Text.Json;

namespace PixelStudio;

/// <summary>
/// 用户设置的持久化。存储位置：
/// %APPDATA%\PixelStudio\settings.json
/// </summary>
public class Settings
{
    /// <summary>新文档默认是否显示网格</summary>
    public bool DefaultShowGrid { get; set; } = true;

    /// <summary>新文档默认笔刷大小（1-20）</summary>
    public int DefaultBrushSize { get; set; } = 1;

    /// <summary>新文档默认不透明度（0-100）</summary>
    public int DefaultOpacity { get; set; } = 100;

    /// <summary>历史记录内存预算（MB）</summary>
    public int HistoryMemoryBudgetMB { get; set; } = 200;

    /// <summary>界面语言（zh-CN / en-US）</summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>主题色 ID（dark / blue / purple / pink / green）</summary>
    public string ThemeId { get; set; } = "dark";

    /// <summary>自定义主题色的 Hex（#RRGGBB）</summary>
    public string CustomThemeColorHex { get; set; } = "#4A90E2";

    /// <summary>用户自定义的快捷键。key = action ID，value = 键位字符串。</summary>
    public Dictionary<string, string> CustomShortcuts { get; set; } = new();

    /// <summary>启动时自动检查更新</summary>
    public bool AutoCheckUpdate { get; set; } = true;

    /// <summary>用户选择"跳过此版本"的版本号</summary>
    public string SkippedUpdateVersion { get; set; } = "";

    /// <summary>是否已通过系统环境检测（通过后不再重复检测）</summary>
    public bool EnvironmentChecked { get; set; } = false;
}

public static class SettingsManager
{
    private static Settings? _current;

    /// <summary>当前设置。首次访问时自动加载。</summary>
    public static Settings Current
    {
        get
        {
            if (_current == null)
                Load();
            return _current!;
        }
    }

    private static string GetFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelStudio");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    /// <summary>从磁盘加载设置。找不到文件就用默认值。</summary>
    public static void Load()
    {
        try
        {
            var path = GetFilePath();
            if (!File.Exists(path))
            {
                _current = new Settings();
                return;
            }

            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<Settings>(json);

            _current = loaded ?? new Settings();

            // 防御性修正：把非法值 clamp 到合法范围
            _current.DefaultBrushSize = Math.Clamp(_current.DefaultBrushSize, 1, 20);
            _current.DefaultOpacity = Math.Clamp(_current.DefaultOpacity, 0, 100);
            _current.HistoryMemoryBudgetMB = Math.Clamp(_current.HistoryMemoryBudgetMB, 50, 500);

            if (string.IsNullOrWhiteSpace(_current.Language))
                _current.Language = "zh-CN";
        }
        catch
        {
            // 解析失败 → 用默认设置，不弹窗
            _current = new Settings();
        }
    }

    /// <summary>把当前设置写回磁盘。</summary>
    public static void Save()
    {
        try
        {
            if (_current == null) return;

            var path = GetFilePath();
            var json = JsonSerializer.Serialize(_current,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // 保存失败不弹窗，避免打扰用户
        }
    }

    /// <summary>恢复默认设置（只更新内存，不自动写盘）。</summary>
    public static void Reset()
    {
        _current = new Settings();
    }
}