namespace PixelStudio.Plugins;

/// <summary>
/// 插件接口。所有插件必须实现这个接口。
/// </summary>
public interface IPlugin
{
    /// <summary>插件名称</summary>
    string Name { get; }

    /// <summary>插件版本（比如 "1.0.0"）</summary>
    string Version { get; }

    /// <summary>作者</summary>
    string Author { get; }

    /// <summary>简短描述</summary>
    string Description { get; }

    /// <summary>
    /// 插件启动时调用。在这里注册菜单项、快捷键等。
    /// </summary>
    void Register(PluginContext ctx);
}