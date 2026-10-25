using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace PixelStudio;

public partial class HomeWindow : Window
{
    public string? OpenFilePath { get; private set; }
    public int NewCanvasWidth { get; private set; } = 0;
    public int NewCanvasHeight { get; private set; } = 0;
    public Color NewBackgroundColor { get; private set; } = Colors.Transparent;

    public HomeWindow()
    {
        InitializeComponent();
        LoadRecentFiles();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        };
    }

    private void LoadRecentFiles()
    {
        RecentList.Items.Clear();

        var files = RecentFilesManager.Load();

        if (files.Count == 0)
        {
            EmptyHint.Visibility = Visibility.Visible;
            RecentList.Visibility = Visibility.Collapsed;
            return;
        }

        EmptyHint.Visibility = Visibility.Collapsed;
        RecentList.Visibility = Visibility.Visible;

        foreach (var f in files)
            RecentList.Items.Add(CreateRecentCard(f.Path));
    }

    private UIElement CreateRecentCard(string path)
    {
        var card = new Border
        {
            Width = 160,
            Height = 190,
            Margin = new Thickness(0, 0, 16, 16),
            CornerRadius = new CornerRadius(8),
            Cursor = Cursors.Hand
        };

        card.SetResourceReference(Border.BackgroundProperty, "Theme_PanelBg");

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var thumbBorder = new Border
        {
            Margin = new Thickness(8, 8, 8, 4),
            CornerRadius = new CornerRadius(6),
            ClipToBounds = true
        };
        thumbBorder.SetResourceReference(Border.BackgroundProperty, "Theme_WindowBg");

        var img = new Image
        {
            Stretch = Stretch.Uniform,
            Margin = new Thickness(4)
        };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.DecodePixelWidth = 140;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            img.Source = bmp;
        }
        catch
        {
            img.Source = null;
        }

        thumbBorder.Child = img;
        Grid.SetRow(thumbBorder, 0);
        grid.Children.Add(thumbBorder);

        var nameBlock = new TextBlock
        {
            Text = Path.GetFileName(path),
            Foreground = Brushes.White,
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(8, 2, 8, 10),
            ToolTip = path
        };
        Grid.SetRow(nameBlock, 1);
        grid.Children.Add(nameBlock);

        card.Child = grid;

        card.MouseLeftButtonDown += (_, _) =>
        {
            if (File.Exists(path))
            {
                OpenFilePath = path;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    LanguageManager.T("Msg_FileNotFound"),
                    LanguageManager.T("Msg_InfoTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RecentFilesManager.Remove(path);
                LoadRecentFiles();
            }
        };

        card.MouseEnter += (_, _) => card.SetResourceReference(Border.BackgroundProperty, "Theme_Border");
        card.MouseLeave += (_, _) => card.SetResourceReference(Border.BackgroundProperty, "Theme_PanelBg");

        return card;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NewCanvasDialog { Owner = this };
        if (dlg.ShowDialog() != true)
            return;

        OpenFilePath = null;
        NewCanvasWidth = dlg.CanvasWidth;
        NewCanvasHeight = dlg.CanvasHeight;
        NewBackgroundColor = dlg.BackgroundColor;
        DialogResult = true;
        Close();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "PNG 图片|*.png|所有文件|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            OpenFilePath = dlg.FileName;
            DialogResult = true;
            Close();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow { Owner = this };
        win.ShowDialog();
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        var win = new HelpWindow { Owner = this };
        win.ShowDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var win = new AboutWindow { Owner = this };
        win.ShowDialog();
    }
}