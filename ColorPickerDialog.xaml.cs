using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PixelStudio;

public partial class ColorPickerDialog : Window
{
    public Color SelectedColor { get; private set; } = Colors.Black;

    private bool _updating = true;

    private double _hue = 0;
    private double _sat = 1;
    private double _val = 1;

    private bool _draggingSv;
    private bool _draggingHue;

    public ColorPickerDialog(Color initial)
    {
        InitializeComponent();

        var (h, s, v) = RgbToHsv(initial);
        _hue = h;
        _sat = s;
        _val = v;

        _updating = false;

        Loaded += (_, _) => UpdateAllVisuals();
        SvCanvas.SizeChanged += (_, _) => UpdateSvIndicator();
        HueCanvas.SizeChanged += (_, _) => UpdateHueIndicator();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        };
    }

    // ==================== SV 色块交互 ====================
    private void SvCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _draggingSv = true;
        SvCanvas.CaptureMouse();
        UpdateSvFromMouse(e);
    }

    private void SvCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_draggingSv) return;
        UpdateSvFromMouse(e);
    }

    private void SvCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _draggingSv = false;
        if (SvCanvas.IsMouseCaptured) SvCanvas.ReleaseMouseCapture();
    }

    private void UpdateSvFromMouse(MouseEventArgs e)
    {
        if (_updating) return;

        var pt = e.GetPosition(SvCanvas);
        double w = Math.Max(SvCanvas.ActualWidth, 1);
        double h = Math.Max(SvCanvas.ActualHeight, 1);

        _sat = Math.Clamp(pt.X / w, 0, 1);
        _val = Math.Clamp(1 - pt.Y / h, 0, 1);

        UpdateAllVisuals();
    }

    // ==================== 色相条交互 ====================
    private void HueCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _draggingHue = true;
        HueCanvas.CaptureMouse();
        UpdateHueFromMouse(e);
    }

    private void HueCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_draggingHue) return;
        UpdateHueFromMouse(e);
    }

    private void HueCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _draggingHue = false;
        if (HueCanvas.IsMouseCaptured) HueCanvas.ReleaseMouseCapture();
    }

    private void UpdateHueFromMouse(MouseEventArgs e)
    {
        if (_updating) return;

        var pt = e.GetPosition(HueCanvas);
        double w = Math.Max(HueCanvas.ActualWidth, 1);

        _hue = Math.Clamp(pt.X / w, 0, 1) * 360;

        UpdateAllVisuals();
    }

    // ==================== RGB 输入 ====================
    private void RgbBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;

        if (!byte.TryParse(RBox.Text, out byte r)) return;
        if (!byte.TryParse(GBox.Text, out byte g)) return;
        if (!byte.TryParse(BBox.Text, out byte b)) return;

        var c = Color.FromRgb(r, g, b);
        var (h, s, v) = RgbToHsv(c);
        _hue = h; _sat = s; _val = v;

        UpdateAllVisuals(skipRgb: true);
    }

    // ==================== Hex 输入 ====================
    private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;

        var text = HexBox.Text.Trim().TrimStart('#');
        if (text.Length != 6) return;

        try
        {
            byte r = Convert.ToByte(text[..2], 16);
            byte g = Convert.ToByte(text.Substring(2, 2), 16);
            byte b = Convert.ToByte(text.Substring(4, 2), 16);

            var c = Color.FromRgb(r, g, b);
            var (h, s, v) = RgbToHsv(c);
            _hue = h; _sat = s; _val = v;

            UpdateAllVisuals(skipRgb: true);
        }
        catch { }
    }

    // ==================== 统一刷新显示 ====================
    private void UpdateAllVisuals(bool skipRgb = false)
    {
        var c = HsvToRgb(_hue, _sat, _val);
        SelectedColor = c;

        _updating = true;

        PreviewCircle.Background = new SolidColorBrush(c);

        var hueColor = HsvToRgb(_hue, 1, 1);
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0)
        };
        brush.GradientStops.Add(new GradientStop(Colors.White, 0));
        brush.GradientStops.Add(new GradientStop(hueColor, 1));
        SvBase.Background = brush;

        UpdateSvIndicator();
        UpdateHueIndicator();

        if (!skipRgb)
        {
            RBox.Text = c.R.ToString();
            GBox.Text = c.G.ToString();
            BBox.Text = c.B.ToString();
        }

        HexBox.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        _updating = false;
    }

    private void UpdateSvIndicator()
    {
        if (SvCanvas.ActualWidth <= 0 || SvCanvas.ActualHeight <= 0) return;

        double x = _sat * SvCanvas.ActualWidth;
        double y = (1 - _val) * SvCanvas.ActualHeight;

        Canvas.SetLeft(SvIndicator, x - SvIndicator.Width / 2);
        Canvas.SetTop(SvIndicator, y - SvIndicator.Height / 2);
    }

    private void UpdateHueIndicator()
    {
        if (HueCanvas.ActualWidth <= 0) return;

        double x = (_hue / 360.0) * HueCanvas.ActualWidth;

        Canvas.SetLeft(HueIndicator, x - HueIndicator.Width / 2);
        Canvas.SetTop(HueIndicator, (HueCanvas.ActualHeight - HueIndicator.Height) / 2);
    }

    // ==================== 按钮 ====================
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ==================== HSV ↔ RGB ====================
    private static (double h, double s, double v) RgbToHsv(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double d = max - min;

        double h = 0;
        if (d != 0)
        {
            if (max == r) h = 60 * (((g - b) / d) % 6);
            else if (max == g) h = 60 * ((b - r) / d + 2);
            else h = 60 * ((r - g) / d + 4);
        }
        if (h < 0) h += 360;

        double s = max == 0 ? 0 : d / max;
        double v = max;

        return (h, s, v);
    }

    private static Color HsvToRgb(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360;
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }
}