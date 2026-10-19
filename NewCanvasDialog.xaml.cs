using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PixelStudio;

public partial class NewCanvasDialog : Window
{
    public int CanvasWidth { get; private set; } = 64;
    public int CanvasHeight { get; private set; } = 64;

    /// <summary>
    /// 用户选择的背景色（A=0 表示透明）。
    /// </summary>
    public Color BackgroundColor { get; private set; } = Colors.Transparent;

    private Color _customColor = Color.FromRgb(0x4A, 0x90, 0xE2);
    private bool _updating = true;

    public NewCanvasDialog()
    {
        InitializeComponent();

        _updating = false;
        UpdatePreview();

        // 窗口尺寸变化时也要刷新预览
        PreviewArea.SizeChanged += (_, _) => UpdatePreview();

        // Enter 确定 / Esc 取消
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Ok_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
        };
    }

    // ==================== 预设比例 ====================
    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string tag) return;

        var parts = tag.Split(':');
        if (parts.Length != 2) return;
        if (!int.TryParse(parts[0], out int rw)) return;
        if (!int.TryParse(parts[1], out int rh)) return;

        if (!int.TryParse(WidthBox.Text, out int w) || w <= 0)
            w = 64;

        int h = (int)Math.Round((double)w * rh / rw);
        h = Math.Clamp(h, 1, 2048);

        _updating = true;
        WidthBox.Text = w.ToString();
        HeightBox.Text = h.ToString();
        _updating = false;

        UpdatePreview();
    }

    // ==================== 尺寸变化 ====================
    private void SizeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (PreviewBox == null || PreviewArea == null) return;

        int w = 64, h = 64;
        if (int.TryParse(WidthBox?.Text, out int pw) && pw > 0) w = pw;
        if (int.TryParse(HeightBox?.Text, out int ph) && ph > 0) h = ph;

        double maxSize = Math.Min(PreviewArea.ActualWidth, PreviewArea.ActualHeight) - 20;
        if (maxSize <= 20) maxSize = 180;

        double ratio = (double)w / h;
        double boxW, boxH;
        if (ratio >= 1)
        {
            boxW = maxSize;
            boxH = maxSize / ratio;
        }
        else
        {
            boxH = maxSize;
            boxW = maxSize * ratio;
        }

        PreviewBox.Width = Math.Max(4, boxW);
        PreviewBox.Height = Math.Max(4, boxH);

        // 预览底色跟随所选背景色
        PreviewBox.Background = new SolidColorBrush(GetCurrentBgColor());

        if (PreviewSizeLabel != null)
            PreviewSizeLabel.Text = $"{w} × {h}";
    }

    // ==================== 背景色 ====================
    private void BgRadio_Changed(object sender, RoutedEventArgs e)
    {
        if (PickCustomColorButton == null) return;

        // 选中"自定义"时才显示"选择自定义颜色"按钮
        PickCustomColorButton.Visibility = (BgCustomRadio.IsChecked == true)
            ? Visibility.Visible
            : Visibility.Collapsed;

        UpdatePreview();
    }

    private void PickCustomColor_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ColorPickerDialog(_customColor) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _customColor = dlg.SelectedColor;
            if (CustomColorChip != null)
                CustomColorChip.Background = new SolidColorBrush(_customColor);
            UpdatePreview();
        }
    }

    private Color GetCurrentBgColor()
    {
        if (BgWhiteRadio?.IsChecked == true) return Colors.White;
        if (BgBlackRadio?.IsChecked == true) return Colors.Black;
        if (BgCustomRadio?.IsChecked == true) return _customColor;
        return Colors.Transparent;
    }

    // ==================== 按钮 ====================
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        int w = 64, h = 64;

        if (int.TryParse(WidthBox.Text, out int pw) && pw > 0 && pw <= 2048)
            w = pw;

        if (int.TryParse(HeightBox.Text, out int ph) && ph > 0 && ph <= 2048)
            h = ph;

        CanvasWidth = w;
        CanvasHeight = h;
        BackgroundColor = GetCurrentBgColor();

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}