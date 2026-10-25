using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PixelStudio;

public partial class App : Application
{
    private const int MinShowMs = 300;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        Directory.CreateDirectory(Path.Combine(exeDir, "Themes"));
        Directory.CreateDirectory(Path.Combine(exeDir, "Languages"));
        Directory.CreateDirectory(Path.Combine(exeDir, "Plugins"));

        SettingsManager.Load();
        ShortcutManager.RebuildCache();
        LanguageManager.LoadExternalPacks();
        LanguageManager.SetLanguage(SettingsManager.Current.Language);

        if (!SettingsManager.Current.EnvironmentChecked)
        {
            if (!EnvironmentChecker.IsSupported())
            {
                var errWin = new EnvironmentErrorWindow();
                errWin.ShowDialog();
                Shutdown();
                return;
            }

            SettingsManager.Current.EnvironmentChecked = true;
            SettingsManager.Save();
        }

        Plugins.PluginHost.LoadAll();
        ThemeManager.LoadExternalThemes();
        ThemeManager.ApplyTheme(SettingsManager.Current.ThemeId);

        var sw = Stopwatch.StartNew();
        var splash = new SplashWindow();
        splash.Show();

        try
        {
            await Dispatcher.Yield(DispatcherPriority.Render);

            splash.SetStatus("正在初始化...");
            splash.SetProgress(0.30);
            await Dispatcher.Yield(DispatcherPriority.Background);

            splash.SetStatus("正在准备界面...");
            splash.SetProgress(0.65);

            var elapsed = sw.ElapsedMilliseconds;
            long finalShowMs = Math.Max(MinShowMs, Math.Min(elapsed + 200, 1200));

            splash.SetStatus($"启动完成 · {elapsed} ms");
            splash.SetProgress(1.0);

            var remaining = finalShowMs - sw.ElapsedMilliseconds;
            if (remaining > 0)
                await Task.Delay((int)remaining);

            splash.FadeOutAndClose();
            await Task.Delay(320);

            var home = new HomeWindow();
            bool? homeResult = home.ShowDialog();

            if (homeResult != true)
            {
                Shutdown();
                return;
            }

            var main = new MainWindow();

            if (!string.IsNullOrEmpty(home.OpenFilePath))
            {
                main.PendingOpenFile = home.OpenFilePath;
            }
            else if (home.NewCanvasWidth > 0 && home.NewCanvasHeight > 0)
            {
                main.PendingNewWidth = home.NewCanvasWidth;
                main.PendingNewHeight = home.NewCanvasHeight;
                main.PendingNewBackground = home.NewBackgroundColor;
            }

            MainWindow = main;
            main.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"启动失败：\n\n{ex.GetType().Name}\n{ex.Message}\n\n{ex.StackTrace}",
                "启动错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            splash.Close();
            Shutdown();
        }
    }
}