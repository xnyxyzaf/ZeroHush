using System.Windows;
using System.Windows.Input;

namespace PixelStudio;

public partial class RestartDialog : Window
{
    /// <summary>用户是否选择了"立即重启"</summary>
    public bool ShouldRestart { get; private set; } = false;

    public RestartDialog()
    {
        InitializeComponent();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                ShouldRestart = false;
                DialogResult = false;
                e.Handled = true;
            }
        };
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void RestartNow_Click(object sender, RoutedEventArgs e)
    {
        ShouldRestart = true;
        DialogResult = true;
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        ShouldRestart = false;
        DialogResult = false;
    }
}