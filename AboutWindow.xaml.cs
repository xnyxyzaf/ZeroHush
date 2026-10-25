using System.Windows;
using System.Windows.Input;

namespace PixelStudio;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Close();
}