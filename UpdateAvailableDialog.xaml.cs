using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace PixelStudio;

public partial class UpdateAvailableDialog : Window
{
    private readonly UpdateChecker.UpdateInfo _info;

    public bool ShouldSkipThisVersion { get; private set; } = false;

    public UpdateAvailableDialog(UpdateChecker.UpdateInfo info)
    {
        InitializeComponent();

        _info = info;

        CurrentVersionText.Text = "v" + UpdateChecker.CurrentVersion;
        LatestVersionText.Text = "v" + info.LatestVersion;

        ReleaseNotesText.Text = string.IsNullOrWhiteSpace(info.ReleaseNotes)
            ? LanguageManager.T("Update_NoNotes")
            : info.ReleaseNotes;

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        };
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Download_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _info.DownloadUrl,
                UseShellExecute = true
            });
        }
        catch { }

        DialogResult = true;
        Close();
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        ShouldSkipThisVersion = true;
        DialogResult = false;
        Close();
    }
}