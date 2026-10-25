using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;

namespace PixelStudio.Plugins;

public static class PluginHost
{
    public const string AppVersion = "1.0.0-beta";

    public static List<LoadedPlugin> LoadedPlugins { get; } = new();

    public static string PluginsDirectory
    {
        get
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(exeDir, "Plugins");
        }
    }

    public static void LoadAll()
    {
        LoadedPlugins.Clear();

        try
        {
            if (!Directory.Exists(PluginsDirectory))
            {
                Directory.CreateDirectory(PluginsDirectory);
                return;
            }

            var dllFiles = Directory.GetFiles(PluginsDirectory, "*.dll");

            foreach (var dll in dllFiles)
            {
                try
                {
                    LoadOnePlugin(dll);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private static void LoadOnePlugin(string dllPath)
    {
        Assembly asm;
        try
        {
            asm = Assembly.LoadFrom(dllPath);
        }
        catch
        {
            return;
        }

        Type[] types;
        try
        {
            types = asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = Array.FindAll(ex.Types, t => t != null)!;
        }
        catch
        {
            return;
        }

        foreach (var type in types)
        {
            if (type == null) continue;
            if (type.IsAbstract) continue;
            if (type.IsInterface) continue;
            if (!typeof(IPlugin).IsAssignableFrom(type)) continue;

            IPlugin? plugin = null;
            try
            {
                plugin = Activator.CreateInstance(type) as IPlugin;
            }
            catch
            {
                continue;
            }

            if (plugin == null) continue;

            var ctx = new PluginContext(AppVersion, PluginsDirectory);

            try
            {
                plugin.Register(ctx);
            }
            catch
            {
                continue;
            }

            LoadedPlugins.Add(new LoadedPlugin
            {
                Name = SafeGet(() => plugin.Name),
                Version = SafeGet(() => plugin.Version),
                Author = SafeGet(() => plugin.Author),
                Description = SafeGet(() => plugin.Description),
                FilePath = dllPath,
                Context = ctx
            });
        }
    }

    private static string SafeGet(Func<string> getter)
    {
        try { return getter() ?? ""; }
        catch { return ""; }
    }
}