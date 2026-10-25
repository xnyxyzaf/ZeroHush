using System.Collections.Generic;

namespace PixelStudio;

public class LanguagePack
{
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public string Author { get; set; } = "";

    public string Version { get; set; } = "";

    public Dictionary<string, string> Strings { get; set; } = new();
}