using System;
using System.Windows;
using System.Windows.Input;

namespace PixelStudio;

public partial class EnvironmentErrorWindow : Window
{
    public EnvironmentErrorWindow()
    {
        InitializeComponent();

        MessageText.Text = LanguageManager.T("Env_Message");

        VersionText.Text = LanguageManager.T("Env_CurrentVersion")
                           + " " + EnvironmentChecker.GetCurrentVersionString();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                e.Handled = true;
        };
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}