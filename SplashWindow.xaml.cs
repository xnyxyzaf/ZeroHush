using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PixelStudio;

public partial class SplashWindow : Window
{
    private bool _isClosing;

    public SplashWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 卡片快速淡入（80ms）
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(80),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        RootCard.BeginAnimation(OpacityProperty, fadeIn);

        // Logo 轻微放大（180ms）
        var scaleIn = new DoubleAnimation
        {
            From = 0.92,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
        LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
    }

    /// <summary>设置状态文字</summary>
    public void SetStatus(string text)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetStatus(text));
            return;
        }
        if (StatusText != null)
            StatusText.Text = text;
    }

    /// <summary>设置进度 0.0 - 1.0</summary>
    public void SetProgress(double value)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => SetProgress(value));
            return;
        }

        if (SplashProgress == null) return;

        value = Math.Max(0, Math.Min(1, value));

        var anim = new DoubleAnimation
        {
            To = value,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        SplashProgress.BeginAnimation(
            System.Windows.Controls.Primitives.RangeBase.ValueProperty, anim);
    }

    /// <summary>淡出并关闭窗口</summary>
    public void FadeOutAndClose()
    {
        if (_isClosing) return;
        _isClosing = true;

        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(280),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (_, _) =>
        {
            try { Close(); }
            catch { }
        };

        RootCard.BeginAnimation(OpacityProperty, fadeOut);
    }
}