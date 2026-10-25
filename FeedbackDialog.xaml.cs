using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PixelStudio;

public partial class FeedbackDialog : Window
{
    private const string ContactEmail = "zeromubug_feedback@163.com";

    private const string GitHubIssueUrl = "https://github.com/xnyxyzaf/ZeroHush/issues/new";

    public FeedbackDialog()
    {
        InitializeComponent();

        TypeCombo.SelectedIndex = 0;
        SystemInfoText.Text = BuildSystemInfo();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        };

        Loaded += (_, _) => DescBox.Focus();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private static string BuildSystemInfo()
    {
        var sb = new StringBuilder();

        try
        {
            var proc = Process.GetCurrentProcess();
            double memoryMB = proc.PrivateMemorySize64 / 1024.0 / 1024.0;

            sb.AppendLine($"ZeroHush: v1.0.0-beta");
            sb.AppendLine($"OS: {Environment.OSVersion.VersionString}");
            sb.AppendLine($"Runtime: {Environment.Version}");
            sb.AppendLine($"Memory: {memoryMB:F1} MB");
            sb.AppendLine($"Arch: {(Environment.Is64BitProcess ? "x64" : "x86")}");
        }
        catch
        {
            sb.AppendLine("(system info unavailable)");
        }

        return sb.ToString().TrimEnd();
    }

    private string BuildFeedbackText()
    {
        var sb = new StringBuilder();

        var typeText = (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

        sb.AppendLine("=== ZeroHush Feedback ===");
        sb.AppendLine();
        sb.AppendLine($"Type: {typeText}");
        sb.AppendLine();
        sb.AppendLine("Description:");
        sb.AppendLine(string.IsNullOrWhiteSpace(DescBox.Text) ? "(empty)" : DescBox.Text);
        sb.AppendLine();
        sb.AppendLine("--- System Info ---");
        sb.AppendLine(SystemInfoText.Text);

        return sb.ToString();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(BuildFeedbackText());

            var original = LanguageManager.T("Feedback_CopyAll");
            var copied = LanguageManager.T("Feedback_Copied");

            if (sender is Button btn)
            {
                btn.Content = copied;
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1.5)
                };
                timer.Tick += (_, _) =>
                {
                    btn.Content = original;
                    timer.Stop();
                };
                timer.Start();
            }
        }
        catch
        {
            MessageBox.Show("复制失败，请手动选中文本复制。",
                LanguageManager.T("Msg_ErrorTitle"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenMail_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(BuildFeedbackText());
        }
        catch { }

        try
        {
            var subject = $"ZeroHush Feedback ({TypeCombo.SelectedIndex})";
            var body = "请在此处粘贴（Ctrl+V）刚才复制的内容。\n\n";

            var mailto = $"mailto:{ContactEmail}" +
                         $"?subject={Uri.EscapeDataString(subject)}" +
                         $"&body={Uri.EscapeDataString(body)}";

            Process.Start(new ProcessStartInfo
            {
                FileName = mailto,
                UseShellExecute = true
            });
        }
        catch
        {
            MessageBox.Show(
                $"未找到默认邮件客户端。\n\n请手动发送到：{ContactEmail}",
                LanguageManager.T("Msg_InfoTitle"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(BuildFeedbackText());
        }
        catch { }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubIssueUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}