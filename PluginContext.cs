using System;
using System.Collections.Generic;

namespace PixelStudio.Plugins;

public class PluginContext
{
    public string AppVersion { get; }

    public string PluginsDirectory { get; }

    internal readonly List<PluginMenuItem> MenuItems = new();

    internal PluginContext(string appVersion, string pluginsDir)
    {
        AppVersion = appVersion;
        PluginsDirectory = pluginsDir;
    }

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

public class LoadedPlugin
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string FilePath { get; set; } = "";
    public PluginContext? Context { get; set; }
}