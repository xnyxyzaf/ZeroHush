using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;

namespace PixelStudio.Plugins;

/// <summary>
/// 插件加载器。启动时扫描 Plugins 文件夹，加载所有 DLL。
/// </summary>
public static class PluginHost
{
    public const string AppVersion = "1.0.0-beta";

    public static List<LoadedPlugin> LoadedPlugins { get; } = new();

    /// <summary>Plugins 文件夹路径（和 exe 同目录）</summary>
    public static string PluginsDirectory
    {
        get
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(exeDir, "Plugins");
        }
    }

    /// <summary>
    /// 扫描并加载所有插件。启动时调用一次。
    /// 任何插件出错都不影响主程序和其他插件。
    /// </summary>
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
                    // 单个插件坏了，不影响其他
                }
            }
        }
        catch
        {
            // 整个扫描流程出错也静默忽略
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
            // 部分类型加载失败，取能加载的那些
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
                // 插件内部报错，跳过
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