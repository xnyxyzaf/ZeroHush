namespace PixelStudio.Plugins;

public interface IPlugin
{
    string Name { get; }

    string Version { get; }

    string Author { get; }

    string Description { get; }

    void Register(PluginContext ctx);
}