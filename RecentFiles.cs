using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PixelStudio;

public class RecentFile
{
    public string Path { get; set; } = "";
    public DateTime LastOpened { get; set; } = DateTime.Now;
}

public static class RecentFilesManager
{
    private const int MaxCount = 20;

    private static string GetFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelStudio");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "recent.json");
    }

    public static List<RecentFile> Load()
    {
        try
        {
            var path = GetFilePath();
            if (!File.Exists(path)) return new List<RecentFile>();

            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<RecentFile>>(json);
            if (list == null) return new List<RecentFile>();

            // 过滤已不存在的文件
            list = list.Where(f => !string.IsNullOrEmpty(f.Path) && File.Exists(f.Path)).ToList();
            return list.OrderByDescending(f => f.LastOpened).ToList();
        }
        catch
        {
            return new List<RecentFile>();
        }
    }

    public static void Add(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var list = Load();
            list.RemoveAll(f => string.Equals(f.Path, filePath, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, new RecentFile { Path = filePath, LastOpened = DateTime.Now });

            if (list.Count > MaxCount)
                list = list.Take(MaxCount).ToList();

            Save(list);
        }
        catch { }
    }

    public static void Remove(string filePath)
    {
        try
        {
            var list = Load();
            list.RemoveAll(f => string.Equals(f.Path, filePath, StringComparison.OrdinalIgnoreCase));
            Save(list);
        }
        catch { }
    }

    public static void Clear()
    {
        try { Save(new List<RecentFile>()); }
        catch { }
    }

    private static void Save(List<RecentFile> list)
    {
        try
        {
            var path = GetFilePath();
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }
}