using System;
using System.Windows;
using System.Windows.Input;

namespace PixelStudio;

public partial class EnvironmentErrorWindow : Window
{
    public EnvironmentErrorWindow()
    {
        InitializeComponent();

        // 设置消息
        MessageText.Text = LanguageManager.T("Env_Message");

        // 显示当前系统版本
        VersionText.Text = LanguageManager.T("Env_CurrentVersion")
                           + " " + EnvironmentChecker.GetCurrentVersionString();

        // 不允许用户通过 Esc 关掉——只能点确定
        // 因为关闭这个窗口 = 关闭软件
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
        // 关闭软件
        Application.Current.Shutdown();
    }
}