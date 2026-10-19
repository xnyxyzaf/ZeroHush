using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace PixelStudio;

/// <summary>
/// 主题管理器。支持：内置主题 + 自定义颜色 + 外部主题包。
/// </summary>
public static class ThemeManager
{
    public class Theme
    {
        public string Id { get; set; } = "";
        public string NameKey { get; set; } = "";
        public string DisplayName { get; set; } = "";   // 用于外部主题（直接显示名字）
        public Color WindowBg { get; set; }
        public Color PanelBg { get; set; }
        public Color Accent { get; set; }
        public bool IsExternal { get; set; } = false;

        public string GetDisplayName()
        {
            if (IsExternal) return DisplayName;
            return LanguageManager.T(NameKey);
        }
    }

    /// <summary>内置主题</summary>
    public static readonly List<Theme> BuiltInThemes = new()
    {
        new Theme
        {
            Id = "dark", NameKey = "Theme_Dark",
            WindowBg = Color.FromRgb(0x1E, 0x1E, 0x1E),
            PanelBg = Color.FromRgb(0x26, 0x26, 0x26),
            Accent = Color.FromRgb(0x4A, 0x90, 0xE2)
        },
        new Theme
        {
            Id = "blue", NameKey = "Theme_Blue",
            WindowBg = Color.FromRgb(0x14, 0x1C, 0x2C),
            PanelBg = Color.FromRgb(0x1C, 0x26, 0x38),
            Accent = Color.FromRgb(0x4A, 0x90, 0xE2)
        },
        new Theme
        {
            Id = "purple", NameKey = "Theme_Purple",
            WindowBg = Color.FromRgb(0x1C, 0x14, 0x28),
            PanelBg = Color.FromRgb(0x26, 0x1C, 0x34),
            Accent = Color.FromRgb(0x9B, 0x59, 0xB6)
        },
        new Theme
        {
            Id = "pink", NameKey = "Theme_Pink",
            WindowBg = Color.FromRgb(0x26, 0x16, 0x20),
            PanelBg = Color.FromRgb(0x32, 0x1E, 0x2C),
            Accent = Color.FromRgb(0xE9, 0x1E, 0x63)
        },
        new Theme
        {
            Id = "green", NameKey = "Theme_Green",
            WindowBg = Color.FromRgb(0x14, 0x20, 0x18),
            PanelBg = Color.FromRgb(0x1C, 0x2C, 0x20),
            Accent = Color.FromRgb(0x2E, 0xCC, 0x71)
        },
    };

    /// <summary>外部主题包（从 Themes 文件夹加载）</summary>
    public static readonly List<Theme> ExternalThemes = new();

    public static string CurrentThemeId { get; private set; } = "dark";

    /// <summary>所有可用主题（内置 + 外部）</summary>
    public static List<Theme> AllThemes
    {
        get
        {
            var list = new List<Theme>();
            list.AddRange(BuiltInThemes);
            list.AddRange(ExternalThemes);
            return list;
        }
    }

    // ==================== 外部主题包加载 ====================
    public static void LoadExternalThemes()
    {
        ExternalThemes.Clear();

        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var dirs = new List<string>
        {
            Path.Combine(exeDir, "Themes"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PixelStudio", "Themes")
        };

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in BuiltInThemes) usedIds.Add(t.Id);
        usedIds.Add("custom"); // 保留

        foreach (var dir in dirs)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    if (dir.StartsWith(exeDir, StringComparison.OrdinalIgnoreCase))
                        Directory.CreateDirectory(dir);
                    continue;
                }

                var files = Directory.GetFiles(dir, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var pack = JsonSerializer.Deserialize<ThemePack>(json, options);

                        if (pack == null) continue;
                        if (string.IsNullOrWhiteSpace(pack.Id)) continue;
                        if (string.IsNullOrWhiteSpace(pack.WindowBg)) continue;
                        if (string.IsNullOrWhiteSpace(pack.PanelBg)) continue;
                        if (string.IsNullOrWhiteSpace(pack.Accent)) continue;

                        // ID 不能跟内置或已加载的冲突
                        if (usedIds.Contains(pack.Id)) continue;

                        var wb = ParseHex(pack.WindowBg);
                        var pb = ParseHex(pack.PanelBg);
                        var ac = ParseHex(pack.Accent);

                        ExternalThemes.Add(new Theme
                        {
                            Id = pack.Id,
                            DisplayName = string.IsNullOrWhiteSpace(pack.Name) ? pack.Id : pack.Name,
                            WindowBg = wb,
                            PanelBg = pb,
                            Accent = ac,
                            IsExternal = true
                        });

                        usedIds.Add(pack.Id);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    // ==================== 获取主题 ====================
    public static Theme Get(string id)
    {
        if (id == "custom")
            return BuildCustomTheme();

        foreach (var t in BuiltInThemes)
            if (t.Id == id) return t;

        foreach (var t in ExternalThemes)
            if (t.Id == id) return t;

        return BuiltInThemes[0];
    }

    private static Theme BuildCustomTheme()
    {
        var baseColor = ParseHex(SettingsManager.Current.CustomThemeColorHex);

        return new Theme
        {
            Id = "custom",
            NameKey = "Theme_Custom",
            WindowBg = Dim(baseColor, 0.10),
            PanelBg = Dim(baseColor, 0.16),
            Accent = baseColor
        };
    }

    private static Color Dim(Color c, double factor)
    {
        byte r = (byte)Math.Clamp(0x12 + c.R * factor, 0x12, 0xFF);
        byte g = (byte)Math.Clamp(0x12 + c.G * factor, 0x12, 0xFF);
        byte b = (byte)Math.Clamp(0x12 + c.B * factor, 0x12, 0xFF);
        return Color.FromRgb(r, g, b);
    }

    private static Color ParseHex(string hex)
    {
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
                return Color.FromRgb(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
        }
        catch { }
        return Color.FromRgb(0x4A, 0x90, 0xE2);
    }

    // ==================== 应用主题 ====================
    public static void ApplyTheme(string id)
    {
        var theme = Get(id);
        CurrentThemeId = id;

        var res = Application.Current.Resources;

        res["Theme_WindowBg"] = MakeBrush(theme.WindowBg);
        res["Theme_PanelBg"] = MakeBrush(theme.PanelBg);
        res["Theme_Accent"] = MakeBrush(theme.Accent);
        res["Theme_WindowBg_Semi"] = MakeBrush(Color.FromArgb(0xCC, theme.WindowBg.R, theme.WindowBg.G, theme.WindowBg.B));
        res["Theme_PanelBg_Semi"] = MakeBrush(Color.FromArgb(0xCC, theme.PanelBg.R, theme.PanelBg.G, theme.PanelBg.B));
    }

    public static string GetAcrylicColorHex()
    {
        var t = Get(CurrentThemeId);
        return $"#CC{t.WindowBg.R:X2}{t.WindowBg.G:X2}{t.WindowBg.B:X2}";
    }

    private static SolidColorBrush MakeBrush(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}