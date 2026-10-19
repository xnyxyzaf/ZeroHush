using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PixelStudio;

public partial class SettingsWindow : Window
{
    private bool _initializing = true;
    private string _originalThemeId = "dark";
    private string _originalCustomColorHex = "#4A90E2";

    public SettingsWindow()
    {
        InitializeComponent();

        _originalThemeId = SettingsManager.Current.ThemeId;
        _originalCustomColorHex = SettingsManager.Current.CustomThemeColorHex;

        LoadLanguageCombo();
        LoadCurrent();
        BuildThemeList();
        BuildPluginList();
        BuildShortcutList();

        _initializing = false;
        UpdateMemoryHint();

        if (NavList.Items.Count > 0)
            NavList.SelectedIndex = 0;

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                SettingsManager.Current.CustomThemeColorHex = _originalCustomColorHex;
                ThemeManager.ApplyTheme(_originalThemeId);
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        };
    }

    // ==================== 左侧导航 ====================
    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PanelGeneral == null || PanelPersonalization == null ||
            PanelPerformance == null || PanelShortcuts == null ||
            PanelPlugins == null || PanelThanks == null || PanelAbout == null)
            return;

        PanelGeneral.Visibility = Visibility.Collapsed;
        PanelPersonalization.Visibility = Visibility.Collapsed;
        PanelPerformance.Visibility = Visibility.Collapsed;
        PanelShortcuts.Visibility = Visibility.Collapsed;
        PanelPlugins.Visibility = Visibility.Collapsed;
        PanelThanks.Visibility = Visibility.Collapsed;
        PanelAbout.Visibility = Visibility.Collapsed;

        switch (NavList.SelectedIndex)
        {
            case 0: PanelGeneral.Visibility = Visibility.Visible; break;
            case 1: PanelPersonalization.Visibility = Visibility.Visible; break;
            case 2: PanelPerformance.Visibility = Visibility.Visible; break;
            case 3: PanelShortcuts.Visibility = Visibility.Visible; break;
            case 4: PanelPlugins.Visibility = Visibility.Visible; break;
            case 5: PanelThanks.Visibility = Visibility.Visible; break;
            case 6: PanelAbout.Visibility = Visibility.Visible; break;
        }
    }

    // ==================== 主题列表 ====================
    private void BuildThemeList()
    {
        if (ThemeListPanel == null) return;
        ThemeListPanel.Children.Clear();

        foreach (var theme in ThemeManager.AllThemes)
            ThemeListPanel.Children.Add(CreateThemeCard(theme));

        if (ThemeManager.CurrentThemeId == "custom")
            ThemeListPanel.Children.Add(CreateThemeCard(ThemeManager.Get("custom")));

        UpdateCustomColorSwatch();
    }

    private UIElement CreateThemeCard(ThemeManager.Theme theme)
    {
        bool isSelected = theme.Id == ThemeManager.CurrentThemeId;

        var card = new Border
        {
            Height = 64,
            Width = 480,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 8),
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            CornerRadius = new CornerRadius(8),
            Cursor = Cursors.Hand,
            BorderBrush = isSelected
                ? new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2))
                : new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            BorderThickness = new Thickness(isSelected ? 2 : 1),
            Tag = theme.Id
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var swatches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 16, 0)
        };
        swatches.Children.Add(MakeSwatch(theme.WindowBg));
        swatches.Children.Add(MakeSwatch(theme.PanelBg));
        swatches.Children.Add(MakeSwatch(theme.Accent));
        Grid.SetColumn(swatches, 0);
        grid.Children.Add(swatches);

        var nameText = new TextBlock
        {
            Text = theme.GetDisplayName(),
            Foreground = Brushes.White,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(nameText, 1);
        grid.Children.Add(nameText);

        if (isSelected)
        {
            var check = new TextBlock
            {
                Text = "✓",
                Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2)),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 18, 0)
            };
            Grid.SetColumn(check, 2);
            grid.Children.Add(check);
        }

        card.Child = grid;

        card.MouseLeftButtonDown += (_, _) =>
        {
            ThemeManager.ApplyTheme(theme.Id);
            BuildThemeList();
        };

        return card;
    }

    private Border MakeSwatch(Color c)
    {
        return new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(c),
            Margin = new Thickness(0, 0, 6, 0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            BorderThickness = new Thickness(1)
        };
    }

    private void UpdateCustomColorSwatch()
    {
        if (CustomColorSwatch == null) return;

        var hex = SettingsManager.Current.CustomThemeColorHex;
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            CustomColorSwatch.Background = new SolidColorBrush(c);
        }
        catch
        {
            CustomColorSwatch.Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2));
        }
    }

    private void CustomColorCard_Click(object sender, MouseButtonEventArgs e)
    {
        Color current;
        try
        {
            current = (Color)ColorConverter.ConvertFromString(SettingsManager.Current.CustomThemeColorHex);
        }
        catch
        {
            current = Color.FromRgb(0x4A, 0x90, 0xE2);
        }

        var dlg = new ColorPickerDialog(current) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            var hex = $"#{dlg.SelectedColor.R:X2}{dlg.SelectedColor.G:X2}{dlg.SelectedColor.B:X2}";
            SettingsManager.Current.CustomThemeColorHex = hex;

            ThemeManager.ApplyTheme("custom");
            BuildThemeList();
        }
    }

    // ==================== 快捷键列表 ====================
    private void BuildShortcutList()
    {
        if (ShortcutListPanel == null) return;
        ShortcutListPanel.Children.Clear();

        foreach (var action in ShortcutManager.Actions)
            ShortcutListPanel.Children.Add(CreateShortcutRow(action));
    }

    private UIElement CreateShortcutRow(ShortcutManager.ShortcutAction action)
    {
        var row = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameText = new TextBlock
        {
            Text = LanguageManager.T(action.NameKey),
            Foreground = Brushes.White,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(nameText, 0);
        grid.Children.Add(nameText);

        var keyBtn = new Button
        {
            Content = ShortcutManager.FriendlyKey(ShortcutManager.GetKey(action.Id)),
            Width = 140,
            Height = 28,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2)),
            BorderThickness = new Thickness(0),
            FontSize = 12,
            FontFamily = new FontFamily("Consolas"),
            Cursor = Cursors.Hand,
            Tag = action.Id
        };

        keyBtn.Template = BuildFlatButtonTemplate();
        keyBtn.Click += ShortcutKeyBtn_Click;

        Grid.SetColumn(keyBtn, 1);
        grid.Children.Add(keyBtn);

        row.Child = grid;
        return row;
    }

    private static ControlTemplate BuildFlatButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));

        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content);

        template.VisualTree = border;
        return template;
    }

    private void ShortcutKeyBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string actionId) return;

        var action = ShortcutManager.Actions.Find(a => a.Id == actionId);
        if (action == null) return;

        var actionName = LanguageManager.T(action.NameKey);
        var currentKey = ShortcutManager.GetKey(actionId);

        var dlg = new ShortcutEditDialog(actionName, currentKey) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        var newKey = dlg.ResultKey;
        if (string.IsNullOrEmpty(newKey)) return;

        var conflictId = ShortcutManager.FindConflict(actionId, newKey);
        if (conflictId != null)
        {
            var conflictAction = ShortcutManager.Actions.Find(a => a.Id == conflictId);
            var conflictName = conflictAction != null
                ? LanguageManager.T(conflictAction.NameKey)
                : conflictId;

            var result = MessageBox.Show(
                string.Format(LanguageManager.T("Shortcut_Conflict"), conflictName),
                LanguageManager.T("Common_Confirm"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            ShortcutManager.SetKey(conflictId, "");
        }

        ShortcutManager.SetKey(actionId, newKey);
        SettingsManager.Save();
        BuildShortcutList();
    }

    private void ResetShortcuts_Click(object sender, RoutedEventArgs e)
    {
        ShortcutManager.ResetAll();
        SettingsManager.Save();
        BuildShortcutList();
    }

    // ==================== 插件列表 ====================
    private void BuildPluginList()
    {
        if (PluginListPanel == null) return;
        PluginListPanel.Children.Clear();

        var plugins = Plugins.PluginHost.LoadedPlugins;

        if (PluginCountLabel != null)
            PluginCountLabel.Text = string.Format(
                LanguageManager.T("Plugins_Loaded"), plugins.Count);

        if (plugins.Count == 0)
        {
            var empty = new TextBlock
            {
                Text = LanguageManager.T("Plugins_None"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x6E)),
                FontSize = 12,
                Margin = new Thickness(0, 6, 0, 0)
            };
            PluginListPanel.Children.Add(empty);
            return;
        }

        foreach (var p in plugins)
            PluginListPanel.Children.Add(CreatePluginCard(p));
    }

    private UIElement CreatePluginCard(Plugins.LoadedPlugin plugin)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconBox = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xE2)),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 14, 0)
        };
        var iconText = new TextBlock
        {
            Text = "🧩",
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconBox.Child = iconText;
        Grid.SetColumn(iconBox, 0);
        grid.Children.Add(iconBox);

        var info = new StackPanel();
        Grid.SetColumn(info, 1);

        var nameText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(plugin.Name) ? "未命名插件" : plugin.Name,
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold
        };
        info.Children.Add(nameText);

        string version = string.IsNullOrWhiteSpace(plugin.Version) ? "?" : plugin.Version;
        string author = string.IsNullOrWhiteSpace(plugin.Author) ? "?" : plugin.Author;
        var metaText = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A)),
            FontSize = 11,
            Margin = new Thickness(0, 4, 0, 0),
            Text = $"{LanguageManager.T("Plugins_Version")} {version}   ·   {LanguageManager.T("Plugins_Author")} {author}"
        };
        info.Children.Add(metaText);

        if (!string.IsNullOrWhiteSpace(plugin.Description))
        {
            var descText = new TextBlock
            {
                Text = plugin.Description,
                Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            };
            info.Children.Add(descText);
        }

        var pathText = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x6E)),
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 8, 0, 0),
            Text = $"{LanguageManager.T("Plugins_Path")}: {plugin.FilePath}",
            ToolTip = plugin.FilePath
        };
        info.Children.Add(pathText);

        grid.Children.Add(info);
        card.Child = grid;
        return card;
    }

    private void OpenPluginsFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = Plugins.PluginHost.PluginsDirectory;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"无法打开文件夹：{ex.Message}",
                LanguageManager.T("Msg_ErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    // ==================== 语言 ====================
    private void LoadLanguageCombo()
    {
        LanguageCombo.Items.Clear();
        foreach (var (code, display) in LanguageManager.Available)
        {
            LanguageCombo.Items.Add(new ComboBoxItem
            {
                Content = display,
                Tag = code
            });
        }
    }

    private void LoadCurrent()
    {
        var s = SettingsManager.Current;

        for (int i = 0; i < LanguageCombo.Items.Count; i++)
        {
            if (LanguageCombo.Items[i] is ComboBoxItem item && (string)item.Tag == s.Language)
            {
                LanguageCombo.SelectedIndex = i;
                break;
            }
        }
        if (LanguageCombo.SelectedIndex < 0 && LanguageCombo.Items.Count > 0)
            LanguageCombo.SelectedIndex = 0;

        DefaultShowGridCheck.IsChecked = s.DefaultShowGrid;

        DefaultBrushSizeSlider.Value = Math.Clamp(s.DefaultBrushSize, 1, 20);
        DefaultBrushSizeLabel.Text = ((int)DefaultBrushSizeSlider.Value).ToString();

        DefaultOpacitySlider.Value = Math.Clamp(s.DefaultOpacity, 0, 100);
        DefaultOpacityLabel.Text = $"{(int)DefaultOpacitySlider.Value}%";

        HistoryBudgetSlider.Value = Math.Clamp(s.HistoryMemoryBudgetMB, 50, 500);
        HistoryBudgetLabel.Text = $"{(int)HistoryBudgetSlider.Value} MB";

        AutoCheckUpdateCheck.IsChecked = s.AutoCheckUpdate;
    }

    private void LanguageCombo_Changed(object sender, SelectionChangedEventArgs e) { }

    private void DefaultBrushSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing) return;
        if (DefaultBrushSizeLabel != null)
            DefaultBrushSizeLabel.Text = ((int)e.NewValue).ToString();
    }

    private void DefaultOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing) return;
        if (DefaultOpacityLabel != null)
            DefaultOpacityLabel.Text = $"{(int)e.NewValue}%";
    }

    private void HistoryBudget_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing) return;
        if (HistoryBudgetLabel != null)
            HistoryBudgetLabel.Text = $"{(int)e.NewValue} MB";
        UpdateMemoryHint();
    }

    private void UpdateMemoryHint()
    {
        if (MemoryHintText == null) return;

        int budgetMB = (int)HistoryBudgetSlider.Value;

        string Hint(long bytes)
        {
            long totalBudget = budgetMB * 1024L * 1024;
            int steps = (int)Math.Min(60, Math.Max(5, totalBudget / bytes));
            return steps.ToString();
        }

        long s64 = 64L * 64 * 4;
        long s256 = 256L * 256 * 4;
        long s512 = 512L * 512 * 4;
        long s1024 = 1024L * 1024 * 4;
        long s2048 = 2048L * 2048 * 4;

        MemoryHintText.Text =
            LanguageManager.T("Settings_MemoryEstimate") + "\n" +
            $"64×64 → {Hint(s64)}     " +
            $"256×256 → {Hint(s256)}     " +
            $"512×512 → {Hint(s512)}\n" +
            $"1024×1024 → {Hint(s1024)}     " +
            $"2048×2048 → {Hint(s2048)}";
    }

    // ==================== 检查更新 ====================
    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (CheckUpdateButton == null || CheckUpdateResultText == null) return;

        CheckUpdateButton.IsEnabled = false;
        CheckUpdateResultText.Text = LanguageManager.T("Update_Checking");
        CheckUpdateResultText.Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A));

        try
        {
            var info = await UpdateChecker.CheckAsync();

            if (info == null)
            {
                CheckUpdateResultText.Text = LanguageManager.T("Update_UpToDate");
                CheckUpdateResultText.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
            }
            else
            {
                var dlg = new UpdateAvailableDialog(info) { Owner = this };
                dlg.ShowDialog();

                if (dlg.ShouldSkipThisVersion)
                {
                    SettingsManager.Current.SkippedUpdateVersion = info.LatestVersion;
                    SettingsManager.Save();
                }

                CheckUpdateResultText.Text = "";
            }
        }
        catch
        {
            CheckUpdateResultText.Text = LanguageManager.T("Update_CheckFailed");
            CheckUpdateResultText.Foreground = new SolidColorBrush(Color.FromRgb(0xE9, 0x1E, 0x63));
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
        }
    }

    // ==================== 问题反馈 ====================
    private void OpenFeedback_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new FeedbackDialog { Owner = this };
        dlg.ShowDialog();
    }

    private void CopyEmail_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not string email) return;

        try
        {
            Clipboard.SetText(email);

            var original = LanguageManager.T("About_CopyEmail");
            var copied = LanguageManager.T("About_Copied");

            btn.Content = copied;
            var timer = new DispatcherTimer
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
        catch { }
    }

    // ==================== 底部按钮 ====================
    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            LanguageManager.T("Msg_ConfirmReset"),
            LanguageManager.T("Common_Confirm"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        SettingsManager.Reset();
        ThemeManager.ApplyTheme("dark");
        _initializing = true;
        LoadCurrent();
        BuildThemeList();
        BuildShortcutList();
        _initializing = false;
        UpdateMemoryHint();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        SettingsManager.Current.CustomThemeColorHex = _originalCustomColorHex;
        ThemeManager.ApplyTheme(_originalThemeId);
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var s = SettingsManager.Current;
        s.DefaultShowGrid = DefaultShowGridCheck.IsChecked == true;
        s.DefaultBrushSize = (int)DefaultBrushSizeSlider.Value;
        s.DefaultOpacity = (int)DefaultOpacitySlider.Value;
        s.HistoryMemoryBudgetMB = (int)HistoryBudgetSlider.Value;

        s.ThemeId = ThemeManager.CurrentThemeId;
        s.AutoCheckUpdate = AutoCheckUpdateCheck.IsChecked == true;

        string newLang = s.Language;
        if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string code)
            newLang = code;

        bool languageChanged = newLang != s.Language;
        s.Language = newLang;

        SettingsManager.Save();

        DialogResult = true;
        Close();

        if (languageChanged)
        {
            var dlg = new RestartDialog { Owner = this.Owner };
            bool? result = dlg.ShowDialog();

            if (result == true && dlg.ShouldRestart)
            {
                RestartApplication();
            }
        }
    }

    private static void RestartApplication()
    {
        try
        {
            var exePath = Environment.ProcessPath;

            if (!string.IsNullOrEmpty(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
        }
        catch { }

        Application.Current.Shutdown();
    }

    // ==================== 致谢 ====================
    private void OpenThanks_Click(object sender, RoutedEventArgs e)
    {
        var win = new ThanksWindow { Owner = this };
        win.ShowDialog();
    }
}