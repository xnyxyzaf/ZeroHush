using System;
using System.Windows.Markup;

namespace PixelStudio;

public partial class LocExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return LanguageManager.T(Key);
    }
}