using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PixelStudio;

public partial class ThanksWindow : Window
{
    private static readonly (string Name, string Url)[] ThanksList =
    {
        ("小春芹香厨", ""), ("我叫柠檬汁", ""), ("一只虚力竭的区", ""), ("吉戬", ""),
        ("沅自东来啊", ""), ("白洛Briya", ""), ("人在哪_", ""), ("Hoxikogon", ""),
        ("赵四先生想当好人活着", ""), ("我真的很想聊天", ""), ("红烧小聋瞎", ""), ("阳sunxuan86", "")
    };

    public ThanksWindow()
    {
        InitializeComponent();
        RefreshList("");
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshList(SearchBox.Text);

    private void RefreshList(string keyword)
    {
        ListPanel.Children.Clear();
        bool hasKeyword = !string.IsNullOrWhiteSpace(keyword);
        int count = 0;

        foreach (var (name, url) in ThanksList)
        {
            if (hasKeyword && !name.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;
            ListPanel.Children.Add(CreateItem(name, url));
            count++;
        }

        CountLabel.Text = hasKeyword
            ? string.Format(LanguageManager.T("Thanks_CountFiltered"), count)
            : string.Format(LanguageManager.T("Thanks_Count"), count);
    }

    private Border CreateItem(string name, string url)
    {
        var border = new Border
        {
            Width = 110, // 一行4个人的固定宽度
            Height = 100,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Margin = new Thickness(4),
            Cursor = Cursors.Hand,
            ToolTip = name
        };
        border.SetResourceReference(Border.BackgroundProperty, "Theme_PanelBg");

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        var avatar = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        avatar.SetResourceReference(Border.BackgroundProperty, "Theme_Accent");

        var avatarText = new TextBlock
        {
            Text = name.Length > 0 ? name[..1] : "?",
            Foreground = Brushes.White,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        avatar.Child = avatarText;
        stack.Children.Add(avatar);

        var nameText = new TextBlock
        {
            Text = name,
            Foreground = Brushes.White,
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center
        };
        stack.Children.Add(nameText);

        border.Child = stack;

        border.MouseEnter += (_, _) => border.SetResourceReference(Border.BackgroundProperty, "Theme_Border");
        border.MouseLeave += (_, _) => border.SetResourceReference(Border.BackgroundProperty, "Theme_PanelBg");
        border.MouseLeftButtonDown += (_, e) => { e.Handled = true; OpenUrl(name, url); };

        return border;
    }

    private static void OpenUrl(string name, string url)
    {
        string finalUrl = !string.IsNullOrWhiteSpace(url) ? url : $"https://search.bilibili.com/upuser?keyword={Uri.EscapeDataString(name)}";
        try { Process.Start(new ProcessStartInfo { FileName = finalUrl, UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(string.Format(LanguageManager.T("Msg_OpenFailed"), ex.Message), LanguageManager.T("Msg_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}