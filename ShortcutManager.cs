using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace PixelStudio;

public static class ShortcutManager
{
    public class ShortcutAction
    {
        public string Id { get; set; } = "";
        public string NameKey { get; set; } = "";
        public string DefaultKey { get; set; } = "";
    }

    public static readonly List<ShortcutAction> Actions = new()
    {
        new() { Id = "Tool_Pen",     NameKey = "Help_Pen",           DefaultKey = "B" },
        new() { Id = "Tool_Eraser",  NameKey = "Help_Eraser",        DefaultKey = "E" },
        new() { Id = "Tool_Fill",    NameKey = "Help_Fill",          DefaultKey = "G" },
        new() { Id = "Tool_Picker",  NameKey = "Help_Picker",        DefaultKey = "I" },
        new() { Id = "Tool_Line",    NameKey = "Help_Line",          DefaultKey = "L" },
        new() { Id = "Tool_Rect",    NameKey = "Help_Rect",          DefaultKey = "R" },
        new() { Id = "Tool_Ellipse", NameKey = "Help_Ellipse",       DefaultKey = "O" },
        new() { Id = "SwapColors",   NameKey = "Help_SwapColors",    DefaultKey = "X" },

        new() { Id = "BrushSizeDec", NameKey = "Shortcut_BrushSizeDec", DefaultKey = "OemOpenBrackets" },
        new() { Id = "BrushSizeInc", NameKey = "Shortcut_BrushSizeInc", DefaultKey = "OemCloseBrackets" },
        new() { Id = "OpacityDec",   NameKey = "Shortcut_OpacityDec",   DefaultKey = "OemComma" },
        new() { Id = "OpacityInc",   NameKey = "Shortcut_OpacityInc",   DefaultKey = "OemPeriod" },

        new() { Id = "Edit_Undo",    NameKey = "Help_Undo",          DefaultKey = "Ctrl+Z" },
        new() { Id = "Edit_Redo",    NameKey = "Help_Redo",          DefaultKey = "Ctrl+Y" },

        new() { Id = "File_New",     NameKey = "Help_New",           DefaultKey = "Ctrl+N" },
        new() { Id = "File_Open",    NameKey = "Help_Open",          DefaultKey = "Ctrl+O" },
        new() { Id = "File_Save",    NameKey = "Help_Save",          DefaultKey = "Ctrl+S" },
        new() { Id = "File_Close",   NameKey = "Help_CloseDoc",      DefaultKey = "Ctrl+W" },

        new() { Id = "View_ZoomIn",    NameKey = "Help_ZoomIn",      DefaultKey = "Ctrl+OemPlus" },
        new() { Id = "View_ZoomOut",   NameKey = "Help_ZoomOut",     DefaultKey = "Ctrl+OemMinus" },
        new() { Id = "View_ZoomReset", NameKey = "Help_ZoomReset",   DefaultKey = "Ctrl+D0" },
        new() { Id = "View_NextDoc",   NameKey = "Help_SwitchDoc",   DefaultKey = "Ctrl+Tab" },

        new() { Id = "Layer_New",    NameKey = "Layer_New",          DefaultKey = "Ctrl+Shift+N" },
    };

    private static readonly Dictionary<string, string> _keyToAction =
        new(StringComparer.OrdinalIgnoreCase);

    public static string GetKey(string actionId)
    {
        var custom = SettingsManager.Current.CustomShortcuts;
        if (custom != null &&
            custom.TryGetValue(actionId, out var key) &&
            !string.IsNullOrEmpty(key))
            return key;

        foreach (var a in Actions)
            if (a.Id == actionId)
                return a.DefaultKey;

        return "";
    }

    public static void SetKey(string actionId, string keyStr)
    {
        if (SettingsManager.Current.CustomShortcuts == null)
            SettingsManager.Current.CustomShortcuts = new Dictionary<string, string>();

        SettingsManager.Current.CustomShortcuts[actionId] = keyStr ?? "";
        RebuildCache();
    }

    public static void ResetAll()
    {
        SettingsManager.Current.CustomShortcuts = new Dictionary<string, string>();
        RebuildCache();
    }

    public static void RebuildCache()
    {
        _keyToAction.Clear();

        foreach (var a in Actions)
        {
            try
            {
                var key = GetKey(a.Id);
                if (!string.IsNullOrEmpty(key) && !_keyToAction.ContainsKey(key))
                    _keyToAction[key] = a.Id;
            }
            catch
            {
            }
        }
    }

    public static string? FindAction(Key key, ModifierKeys mods)
    {
        var str = KeyToString(key, mods);
        return _keyToAction.TryGetValue(str, out var id) ? id : null;
    }

    public static string? FindConflict(string actionId, string keyStr)
    {
        foreach (var a in Actions)
        {
            if (a.Id == actionId) continue;
            if (string.Equals(GetKey(a.Id), keyStr, StringComparison.OrdinalIgnoreCase))
                return a.Id;
        }
        return null;
    }

    public static string KeyToString(Key key, ModifierKeys mods)
    {
        var parts = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        parts.Add(KeyName(key));
        return string.Join("+", parts);
    }

    public static string KeyName(Key key)
    {
        return key switch
        {
            Key.OemPlus => "OemPlus",
            Key.OemMinus => "OemMinus",
            Key.OemComma => "OemComma",
            Key.OemPeriod => "OemPeriod",
            Key.OemOpenBrackets => "OemOpenBrackets",
            Key.OemCloseBrackets => "OemCloseBrackets",
            Key.OemSemicolon => "OemSemicolon",
            Key.OemQuotes => "OemQuotes",
            Key.OemQuestion => "OemQuestion",
            Key.OemPipe => "OemPipe",
            Key.OemTilde => "OemTilde",
            Key.Space => "Space",
            Key.Enter => "Enter",
            Key.Escape => "Esc",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.Tab => "Tab",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.D0 => "D0",
            Key.D1 => "D1",
            Key.D2 => "D2",
            Key.D3 => "D3",
            Key.D4 => "D4",
            Key.D5 => "D5",
            Key.D6 => "D6",
            Key.D7 => "D7",
            Key.D8 => "D8",
            Key.D9 => "D9",
            Key.NumPad0 => "Num0",
            Key.NumPad1 => "Num1",
            Key.NumPad2 => "Num2",
            Key.NumPad3 => "Num3",
            Key.NumPad4 => "Num4",
            Key.NumPad5 => "Num5",
            Key.NumPad6 => "Num6",
            Key.NumPad7 => "Num7",
            Key.NumPad8 => "Num8",
            Key.NumPad9 => "Num9",
            Key.Add => "Add",
            Key.Subtract => "Subtract",
            Key.Multiply => "Multiply",
            Key.Divide => "Divide",
            Key.F1 => "F1",
            Key.F2 => "F2",
            Key.F3 => "F3",
            Key.F4 => "F4",
            Key.F5 => "F5",
            Key.F6 => "F6",
            Key.F7 => "F7",
            Key.F8 => "F8",
            Key.F9 => "F9",
            Key.F10 => "F10",
            Key.F11 => "F11",
            Key.F12 => "F12",
            _ => key.ToString()
        };
    }

    public static string FriendlyKey(string keyStr)
    {
        if (string.IsNullOrEmpty(keyStr)) return "";

        var parts = keyStr.Split('+');
        var outParts = new List<string>();

        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (i == parts.Length - 1)
                p = FriendlyKeyName(p);
            outParts.Add(p);
        }

        return string.Join("+", outParts);
    }

    private static string FriendlyKeyName(string name)
    {
        return name switch
        {
            "OemPlus" => "+",
            "OemMinus" => "-",
            "OemComma" => ",",
            "OemPeriod" => ".",
            "OemOpenBrackets" => "[",
            "OemCloseBrackets" => "]",
            "OemSemicolon" => ";",
            "OemQuotes" => "'",
            "OemQuestion" => "/",
            "OemPipe" => "\\",
            "OemTilde" => "`",
            "D0" => "0",
            "D1" => "1",
            "D2" => "2",
            "D3" => "3",
            "D4" => "4",
            "D5" => "5",
            "D6" => "6",
            "D7" => "7",
            "D8" => "8",
            "D9" => "9",
            _ => name
        };
    }

    public static bool TryParse(string keyStr, out Key key, out ModifierKeys mods)
    {
        key = Key.None;
        mods = ModifierKeys.None;

        if (string.IsNullOrWhiteSpace(keyStr)) return false;

        var parts = keyStr.Split('+');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var p = parts[i].Trim();
            if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) mods |= ModifierKeys.Control;
            else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) mods |= ModifierKeys.Shift;
            else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) mods |= ModifierKeys.Alt;
        }

        var last = parts[^1].Trim();
        key = ParseKeyName(last);
        return key != Key.None;
    }

    private static Key ParseKeyName(string name)
    {
        switch (name)
        {
            case "OemPlus": return Key.OemPlus;
            case "OemMinus": return Key.OemMinus;
            case "OemComma": return Key.OemComma;
            case "OemPeriod": return Key.OemPeriod;
            case "OemOpenBrackets": return Key.OemOpenBrackets;
            case "OemCloseBrackets": return Key.OemCloseBrackets;
            case "OemSemicolon": return Key.OemSemicolon;
            case "OemQuotes": return Key.OemQuotes;
            case "OemQuestion": return Key.OemQuestion;
            case "OemPipe": return Key.OemPipe;
            case "OemTilde": return Key.OemTilde;
            case "Space": return Key.Space;
            case "Enter": return Key.Enter;
            case "Esc": return Key.Escape;
            case "Backspace": return Key.Back;
            case "Delete": return Key.Delete;
            case "Tab": return Key.Tab;
            case "Up": return Key.Up;
            case "Down": return Key.Down;
            case "Left": return Key.Left;
            case "Right": return Key.Right;
            case "D0": return Key.D0;
            case "D1": return Key.D1;
            case "D2": return Key.D2;
            case "D3": return Key.D3;
            case "D4": return Key.D4;
            case "D5": return Key.D5;
            case "D6": return Key.D6;
            case "D7": return Key.D7;
            case "D8": return Key.D8;
            case "D9": return Key.D9;
            case "Num0": return Key.NumPad0;
            case "Num1": return Key.NumPad1;
            case "Num2": return Key.NumPad2;
            case "Num3": return Key.NumPad3;
            case "Num4": return Key.NumPad4;
            case "Num5": return Key.NumPad5;
            case "Num6": return Key.NumPad6;
            case "Num7": return Key.NumPad7;
            case "Num8": return Key.NumPad8;
            case "Num9": return Key.NumPad9;
            case "Add": return Key.Add;
            case "Subtract": return Key.Subtract;
            case "Multiply": return Key.Multiply;
            case "Divide": return Key.Divide;
            case "F1": return Key.F1;
            case "F2": return Key.F2;
            case "F3": return Key.F3;
            case "F4": return Key.F4;
            case "F5": return Key.F5;
            case "F6": return Key.F6;
            case "F7": return Key.F7;
            case "F8": return Key.F8;
            case "F9": return Key.F9;
            case "F10": return Key.F10;
            case "F11": return Key.F11;
            case "F12": return Key.F12;
        }

        if (Enum.TryParse<Key>(name, out var k))
            return k;

        return Key.None;
    }
}