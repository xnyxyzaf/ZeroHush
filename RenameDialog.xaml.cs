using System.Windows;
using System.Windows.Input;

namespace PixelStudio;

public partial class RenameDialog : Window
{
    public string NewName { get; private set; } = "";

    public RenameDialog(string currentName)
    {
        InitializeComponent();
        NameBox.Text = currentName;

        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Ok_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            DialogResult = false;
            return;
        }
        NewName = name;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}