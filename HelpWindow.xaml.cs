using System.Windows;
using System.Windows.Input;

namespace PixelStudio;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();

        SyncShortcuts();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }

    private void SyncShortcuts()
    {
        PenKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("Tool_Pen"));
        EraserKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("Tool_Eraser"));
        FillKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("Tool_Fill"));
        PickerKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("Tool_Picker"));
        LineKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("Tool_Line"));
        RectKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("Tool_Rect"));
        EllipseKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("Tool_Ellipse"));

        UndoKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("Edit_Undo"));
        RedoKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("Edit_Redo"));
        SwapKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("SwapColors"));

        NewKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("File_New"));
        OpenKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("File_Open"));
        SaveKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("File_Save"));
        CloseKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("File_Close"));

        var zoomIn = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("View_ZoomIn"));
        var zoomOut = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("View_ZoomOut"));
        var zoomReset = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("View_ZoomReset"));
        
        ZoomInKeyText.Text = $"{zoomIn} / {zoomOut}";

        NextDocKeyText.Text = ShortcutManager.FriendlyKey(ShortcutManager.GetKey("View_NextDoc"));
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Close();
}