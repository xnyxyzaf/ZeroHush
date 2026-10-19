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
        ("小春芹香厨", ""),
        ("我叫柠檬汁", ""),
        ("一只虚力竭的区", ""),
        ("吉戬", ""),
        ("沅自东来啊", ""),
        ("白洛Briya", ""),
        ("人在哪_", ""),
        ("Hoxikogon", ""),
        ("赵四先生想当好人活着", ""),
        ("我真的很想聊天", ""),
        ("红烧小聋瞎", ""),
        ("阳sunxuan86", "")
    };

    public ThanksWindow()
    {
        InitializeComponent();
        RefreshList("");

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshList(SearchBox.Text);
    }

    private void RefreshList(string keyword)
    {
        ListPanel.Children.Clear();

        bool hasKeyword = !string.IsNullOrWhiteSpace(keyword);
        int count = 0;

        foreach (var (name, url) in ThanksList)
        {
            if (hasKeyword && !name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                continue;

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
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 6),
            Cursor = Cursors.Hand,
            ToolTip = string.IsNullOrWhiteSpace(url)
                ? LanguageManager.T("Thanks_TipSearch")
                : LanguageManager.T("Thanks_Tip")
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var avatar = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };

        var avatarText = new TextBlock
        {
            Text = name.Length > 0 ? name[..1] : "?",
            Foreground = Brushes.White,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        avatar.Child = avatarText;
        Grid.SetColumn(avatar, 0);
        grid.Children.Add(avatar);

        var nameText = new TextBlock
        {
            Text = name,
            Foreground = Brushes.White,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(nameText, 1);
        grid.Children.Add(nameText);

        var arrow = new TextBlock
        {
            Text = "→",
            Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x6E)),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(arrow, 2);
        grid.Children.Add(arrow);

        border.Child = grid;

        var normalBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26));
        var hoverBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
        border.MouseEnter += (_, _) =>
        {
            border.Background = hoverBrush;
            arrow.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2));
        };
        border.MouseLeave += (_, _) =>
        {
            border.Background = normalBrush;
            arrow.Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x6E));
        };

        border.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            OpenUrl(name, url);
        };

        return border;
    }

    private static void OpenUrl(string name, string url)
    {
        string finalUrl;
        if (!string.IsNullOrWhiteSpace(url))
            finalUrl = url;
        else
            finalUrl = $"https://search.bilibili.com/upuser?keyword={Uri.EscapeDataString(name)}";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = finalUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(LanguageManager.T("Msg_OpenFailed"), ex.Message),
                LanguageManager.T("Msg_ErrorTitle"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}