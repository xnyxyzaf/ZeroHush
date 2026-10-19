using System;
using System.Collections.Generic;

namespace PixelStudio.Plugins;

/// <summary>
/// 插件能调用的 API。
/// 目前只支持：注册一个菜单项。
/// </summary>
public class PluginContext
{
    /// <summary>ZeroHush 版本号，插件可据此判断兼容性</summary>
    public string AppVersion { get; }

    /// <summary>Plugins 文件夹的完整路径</summary>
    public string PluginsDirectory { get; }

    internal readonly List<PluginMenuItem> MenuItems = new();

    internal PluginContext(string appVersion, string pluginsDir)
    {
        AppVersion = appVersion;
        PluginsDirectory = pluginsDir;
    }

    /// <summary>
    /// 在「帮助 → 插件」下加一个菜单项。
    /// </summary>
    public void AddMenuItem(string header, Action onClick)
    {
        if (string.IsNullOrWhiteSpace(header)) return;
        if (onClick == null) return;

        MenuItems.Add(new PluginMenuItem
        {
            Header = header,
            OnClick = onClick
        });
    }
}

internal class PluginMenuItem
{
    public string Header { get; set; } = "";
    public Action? OnClick { get; set; }
}

/// <summary>
/// 一个已加载的插件。
/// </summary>
public class LoadedPlugin
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string FilePath { get; set; } = "";
    public PluginContext? Context { get; set; }
}