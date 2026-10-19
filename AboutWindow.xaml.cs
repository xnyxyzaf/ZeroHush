using System.Windows;
using System.Windows.Input;

namespace PixelStudio;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        // Enter / Esc 都关闭
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Close();
}