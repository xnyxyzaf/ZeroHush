using System;
using System.IO;
using System.Text.Json;

namespace PixelStudio;

public class Settings
{
    public bool DefaultShowGrid { get; set; } = true;

    public int DefaultBrushSize { get; set; } = 1;

    public int DefaultOpacity { get; set; } = 100;

    public int HistoryMemoryBudgetMB { get; set; } = 200;

    public string Language { get; set; } = "zh-CN";

    public string ThemeId { get; set; } = "dark";

    public string CustomThemeColorHex { get; set; } = "#4A90E2";

    public Dictionary<string, string> CustomShortcuts { get; set; } = new();

    public bool AutoCheckUpdate { get; set; } = true;

    public string SkippedUpdateVersion { get; set; } = "";

    public bool EnvironmentChecked { get; set; } = false;
}

public static class SettingsManager
{
    private static Settings? _current;

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

            _current.DefaultBrushSize = Math.Clamp(_current.DefaultBrushSize, 1, 20);
            _current.DefaultOpacity = Math.Clamp(_current.DefaultOpacity, 0, 100);
            _current.HistoryMemoryBudgetMB = Math.Clamp(_current.HistoryMemoryBudgetMB, 50, 500);

            if (string.IsNullOrWhiteSpace(_current.Language))
                _current.Language = "zh-CN";
        }
        catch
        {
            _current = new Settings();
        }
    }

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
        }
    }

    public static void Reset()
    {
        _current = new Settings();
    }
}