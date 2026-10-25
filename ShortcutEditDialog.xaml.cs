using System.Windows;
using System.Windows.Input;

namespace PixelStudio;

public partial class ShortcutEditDialog : Window
{
    public string ResultKey { get; private set; } = "";

    private Key _key = Key.None;
    private ModifierKeys _mods = ModifierKeys.None;

    public ShortcutEditDialog(string actionName, string currentKey)
    {
        InitializeComponent();

        InputMethod.SetIsInputMethodEnabled(this, false);

        ActionNameText.Text = actionName;
        ResultKey = currentKey;

        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += (_, _) => Focus();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        Key key;
        if (e.Key == Key.System)
            key = e.SystemKey;
        else if (e.Key == Key.ImeProcessed)
            key = e.ImeProcessedKey;
        else
            key = e.Key;

        var mods = Keyboard.Modifiers;

        if (key == Key.Escape && mods == ModifierKeys.None)
        {
            DialogResult = false;
            Close();
            return;
        }

        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LWin || key == Key.RWin ||
            key == Key.System || key == Key.ImeProcessed ||
            key == Key.None)
            return;

        _key = key;
        _mods = mods;

        var str = ShortcutManager.KeyToString(key, mods);
        KeyDisplayText.Text = ShortcutManager.FriendlyKey(str);
        OkButton.IsEnabled = true;
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (_key == Key.None) return;

        ResultKey = ShortcutManager.KeyToString(_key, _mods);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}