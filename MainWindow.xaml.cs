using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace PixelStudio;

public partial class MainWindow : Window
{
    private readonly List<Document> _documents = new();
    private Document? _current;
    private readonly HistoryManager _emptyHistory = new();
    private HistoryManager History => _current?.History ?? _emptyHistory;
    private CanvasEngine? Canvas => _current?.Canvas;

    private ToolType _currentTool = ToolType.Pen;
    private Color _foreColor = Colors.Black;
    private Color _backColor = Colors.White;
    private int _brushSize = 1;
    private int _opacity = 100;
    private double _zoom = 1.0;
    private bool _showGrid = true;

    private bool _isDrawing;
    private (int x, int y) _startPixel;
    private (int x, int y) _lastPixel;
    private int _docCounter = 0;

    private byte[]? _dragStartSnapshot;

    private bool _updatingLayerTree = false;
    private bool _updatingHistoryList = false;
    private bool _updatingTabs = false;

    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _memoryTimer;
    private bool _refreshPending = false;

    private bool _updateChecked = false;

    private MirrorMode _mirrorMode = MirrorMode.None;

    private LayerNode? _dragSourceNode;
    private Point _dragStartPoint;
    private bool _isDraggingLayer;
    private LayerNode? _currentDropTarget;
    private DropPosition _currentDropPos = DropPosition.None;

    private readonly BlurEffect _menuBackdropBlur = new BlurEffect
    {
        Radius = 18,
        KernelType = KernelType.Gaussian,
        RenderingBias = RenderingBias.Performance
    };

    public string? PendingOpenFile { get; set; }
    public int PendingNewWidth { get; set; } = 0;
    public int PendingNewHeight { get; set; } = 0;
    public Color PendingNewBackground { get; set; } = Colors.Transparent;

    private static readonly string[] DefaultPalette =
    {
        "#000000", "#FFFFFF", "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF",
        "#800000", "#008000", "#000080", "#808000", "#800080", "#008080", "#C0C0C0", "#808080",
        "#FFA500", "#A52A2A", "#FFC0CB", "#ADD8E6", "#90EE90", "#FFD700", "#FF69B4", "#00CED1",
        "#2F4F4F", "#8B4513", "#228B22", "#4169E1", "#DC143C", "#FF4500", "#9932CC", "#20B2AA"
    };

    public MainWindow()
    {
        InitializeComponent();

        _refreshTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _refreshTimer.Tick += (_, _) =>
        {
            if (_refreshPending)
            {
                _refreshPending = false;
                ForceRefresh();
            }
        };
        _refreshTimer.Start();

        _memoryTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _memoryTimer.Tick += (_, _) => UpdateMemoryDisplay();
        _memoryTimer.Start();

        Loaded += (_, _) =>
        {
            var s = SettingsManager.Current;
            _showGrid = s.DefaultShowGrid;
            _brushSize = Math.Clamp(s.DefaultBrushSize, 1, 20);
            _opacity = Math.Clamp(s.DefaultOpacity, 0, 100);

            if (GridMenuItem != null) GridMenuItem.IsChecked = _showGrid;
            if (ToolBarBrushSize != null) ToolBarBrushSize.Value = _brushSize;
            if (OpacitySlider != null) OpacitySlider.Value = _opacity;

            Native.ApplyAcrylic(this, ThemeManager.GetAcrylicColorHex());
            BuildPalette();
            UpdateColorPreview();

            if (!string.IsNullOrEmpty(PendingOpenFile) && File.Exists(PendingOpenFile))
            {
                var pending = PendingOpenFile;
                PendingOpenFile = null;
                OpenFileByPath(pending);
            }
            else if (PendingNewWidth > 0 && PendingNewHeight > 0)
            {
                int w = PendingNewWidth;
                int h = PendingNewHeight;
                Color bg = PendingNewBackground;
                PendingNewWidth = 0;
                PendingNewHeight = 0;
                PendingNewBackground = Colors.Transparent;
                NewDocument(w, h, bg);
            }
            else
            {
                NewDocument(64, 64);
            }

            BuildPluginMenu();
            UpdateMemoryDisplay();
            StartUpdateCheck();
        };

        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void RequestRefresh() => _refreshPending = true;

    private void ForceRefresh()
    {
        var canvas = Canvas;
        if (canvas == null || CanvasImage == null) return;
        canvas.Refresh();
        var bmp = canvas.Bitmap;
        CanvasImage.Source = null;
        CanvasImage.Source = bmp;
        CanvasImage.InvalidateVisual();
    }

    protected override void OnClosed(EventArgs e)
    {
        _refreshTimer?.Stop();
        _memoryTimer?.Stop();
        base.OnClosed(e);
        Application.Current.Shutdown();
    }

    // ==================== 启动检查更新 ====================
    private async void StartUpdateCheck()
    {
        if (_updateChecked) return;
        if (!SettingsManager.Current.AutoCheckUpdate) return;

        _updateChecked = true;

        await System.Threading.Tasks.Task.Delay(5000);

        try
        {
            var info = await UpdateChecker.CheckAsync();
            if (info == null) return;

            if (SettingsManager.Current.SkippedUpdateVersion == info.LatestVersion)
                return;

            Dispatcher.Invoke(() =>
            {
                var dlg = new UpdateAvailableDialog(info) { Owner = this };
                dlg.ShowDialog();

                if (dlg.ShouldSkipThisVersion)
                {
                    SettingsManager.Current.SkippedUpdateVersion = info.LatestVersion;
                    SettingsManager.Save();
                }
            });
        }
        catch { }
    }

    // ==================== 内存监视 ====================
    private void UpdateMemoryDisplay()
    {
        if (MemoryLabel == null) return;

        try
        {
            var proc = Process.GetCurrentProcess();
            double mb = proc.PrivateMemorySize64 / 1024.0 / 1024.0;
            MemoryLabel.Text = $"{LanguageManager.T("Status_MemoryLabel")}{mb:F1} MB";
        }
        catch
        {
            MemoryLabel.Text = "";
        }
    }

    // ==================== 插件菜单 ====================
    private void BuildPluginMenu()
    {
        if (HelpMenu == null) return;

        var plugins = Plugins.PluginHost.LoadedPlugins;
        if (plugins.Count == 0) return;

        var pluginRoot = new MenuItem
        {
            Header = "插件",
            Foreground = Brushes.White
        };

        foreach (var p in plugins)
        {
            if (p.Context == null) continue;
            if (p.Context.MenuItems.Count == 0) continue;

            var pluginSub = new MenuItem
            {
                Header = string.IsNullOrWhiteSpace(p.Name) ? "未命名插件" : p.Name,
                Foreground = Brushes.White
            };

            foreach (var item in p.Context.MenuItems)
            {
                if (item.OnClick == null) continue;

                var mi = new MenuItem
                {
                    Header = item.Header,
                    Foreground = Brushes.White
                };
                var cb = item.OnClick;
                mi.Click += (_, _) =>
                {
                    try { cb(); }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"插件执行出错：\n{ex.Message}",
                            "插件错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                };
                pluginSub.Items.Add(mi);
            }

            pluginRoot.Items.Add(pluginSub);
        }

        if (pluginRoot.Items.Count == 0) return;

        HelpMenu.Items.Add(new Separator());
        HelpMenu.Items.Add(pluginRoot);
    }

    // ==================== 镜像模式 ====================
    private enum MirrorMode { None, Horizontal, Vertical, Quadrant }

    private void MirrorCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (MirrorCombo?.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
            return;

        if (!Enum.TryParse<MirrorMode>(tag, out var m))
            return;

        _mirrorMode = m;

        if (StatusLabel != null)
        {
            StatusLabel.Text = m switch
            {
                MirrorMode.Horizontal => LanguageManager.T("Mirror_Horizontal"),
                MirrorMode.Vertical => LanguageManager.T("Mirror_Vertical"),
                MirrorMode.Quadrant => LanguageManager.T("Mirror_Quadrant"),
                _ => LanguageManager.T("Mirror_Off")
            };
        }
    }

    private void SetPixelWithMirror(int x, int y, Color c)
    {
        if (Canvas == null) return;

        int w = Canvas.Width;
        int h = Canvas.Height;

        Canvas.SetPixel(x, y, c);

        if (_mirrorMode == MirrorMode.None) return;

        int mx = w - 1 - x;
        int my = h - 1 - y;

        bool doH = _mirrorMode == MirrorMode.Horizontal || _mirrorMode == MirrorMode.Quadrant;
        bool doV = _mirrorMode == MirrorMode.Vertical || _mirrorMode == MirrorMode.Quadrant;

        if (doH && mx != x) Canvas.SetPixel(mx, y, c);
        if (doV && my != y) Canvas.SetPixel(x, my, c);
        if (_mirrorMode == MirrorMode.Quadrant && mx != x && my != y) Canvas.SetPixel(mx, my, c);
    }

    // ==================== 笔刷光标 ====================
    private void UpdateBrushCursor(Point pos, int px, int py, int canvasW, int canvasH)
    {
        if (BrushCursor == null) return;

        if (px < 0 || py < 0 || px >= canvasW || py >= canvasH)
        {
            BrushCursor.Visibility = Visibility.Collapsed;
            return;
        }

        BrushCursor.Visibility = Visibility.Visible;

        double size = _brushSize * _zoom;
        if (size < 4) size = 4;

        BrushCursor.Width = size;
        BrushCursor.Height = size;

        BrushCursor.Margin = new Thickness(
            pos.X - size / 2,
            pos.Y - size / 2,
            0, 0);
    }

    private void HideBrushCursor()
    {
        if (BrushCursor != null)
            BrushCursor.Visibility = Visibility.Collapsed;
    }

    // ==================== 图层缩略图 ====================
    private Border CreateThumbnailImage(Layer layer)
    {
        var border = new Border
        {
            Width = 54,
            Height = 54,
            Margin = new Thickness(2, 0, 8, 0),
            CornerRadius = new CornerRadius(3),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x18)),
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            ClipToBounds = true
        };

        var img = new Image
        {
            Stretch = Stretch.Uniform,
            Margin = new Thickness(1)
        };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);

        var px = layer.PixelsOrNull;
        if (px != null)
        {
            try
            {
                var bmp = BitmapSource.Create(
                    layer.Width, layer.Height, 96, 96,
                    PixelFormats.Bgra32, null,
                    px, layer.Width * 4);
                bmp.Freeze();
                img.Source = bmp;
            }
            catch { }
        }

        border.Child = img;
        return border;
    }

    // ==================== 文档管理 ====================
    private void NewDocument(int w, int h, Color bg = default)
    {
        _docCounter++;
        var doc = new Document(w, h, $"未命名-{_docCounter}");

        if (bg.A > 0)
        {
            var layer = doc.Canvas.ActiveLayer;
            if (layer != null)
            {
                var px = layer.Pixels;
                for (int i = 0; i < px.Length; i += 4)
                {
                    px[i] = bg.B;
                    px[i + 1] = bg.G;
                    px[i + 2] = bg.R;
                    px[i + 3] = bg.A;
                }
                doc.Canvas.Refresh();
            }
        }

        _documents.Add(doc);
        var tab = new TabItem { Header = doc.Title, Tag = doc, DataContext = doc };
        _updatingTabs = true;
        DocumentTabs.Items.Add(tab);
        _updatingTabs = false;
        SwitchDocument(doc);
    }

    public void OpenFileByPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

        try
        {
            var bmp = new BitmapImage(new Uri(filePath));
            int w = bmp.PixelWidth, h = bmp.PixelHeight;

            var doc = new Document(w, h, Path.GetFileNameWithoutExtension(filePath));
            doc.FilePath = filePath;

            var converted = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
            var pixels = new byte[w * h * 4];
            converted.CopyPixels(pixels, w * 4, 0);
            Buffer.BlockCopy(pixels, 0, doc.Canvas.ActiveLayer!.Pixels, 0, pixels.Length);
            doc.Canvas.Refresh();

            _documents.Add(doc);
            var tab = new TabItem { Header = doc.Title, Tag = doc, DataContext = doc };
            _updatingTabs = true;
            DocumentTabs.Items.Add(tab);
            _updatingTabs = false;

            SwitchDocument(doc);
            RecentFilesManager.Add(filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(LanguageManager.T("Msg_OpenFailed"), ex.Message),
                LanguageManager.T("Msg_ErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void SwitchDocument(Document doc)
    {
        _current = doc;
        _updatingTabs = true;
        for (int i = 0; i < DocumentTabs.Items.Count; i++)
        {
            if (DocumentTabs.Items[i] is TabItem t && t.Tag == doc)
            {
                DocumentTabs.SelectedIndex = i;
                break;
            }
        }
        _updatingTabs = false;

        CanvasImage.Source = doc.Canvas.Bitmap;
        _zoom = 1.0;

        doc.History.Changed -= OnHistoryChanged;
        doc.History.Changed += OnHistoryChanged;

        ToolBarBrushSize.Value = _brushSize;
        RefreshLayerTree();
        RefreshHistoryList();
        UpdateCanvasSize();
        UpdateZoomDisplay();
        UpdateUndoRedoState();
        UpdateDocInfo();
        SetupGrid();
        HideBrushCursor();
        UpdateMemoryDisplay();
    }

    private void CloseDocument(Document doc)
    {
        int idx = _documents.IndexOf(doc);
        if (idx < 0) return;
        _documents.RemoveAt(idx);
        _updatingTabs = true;
        for (int i = DocumentTabs.Items.Count - 1; i >= 0; i--)
        {
            if (DocumentTabs.Items[i] is TabItem t && t.Tag == doc)
            {
                DocumentTabs.Items.RemoveAt(i);
                break;
            }
        }
        _updatingTabs = false;
        if (_documents.Count == 0) NewDocument(64, 64);
        else SwitchDocument(_documents[Math.Min(idx, _documents.Count - 1)]);
    }

    private void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingTabs) return;
        if (DocumentTabs.SelectedItem is TabItem t && t.Tag is Document doc)
            SwitchDocument(doc);
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        Document? doc = b.Tag as Document ?? b.DataContext as Document;
        if (doc == null)
        {
            var parent = VisualTreeHelper.GetParent(b);
            while (parent != null && parent is not TabItem)
                parent = VisualTreeHelper.GetParent(parent);
            if (parent is TabItem tab) doc = tab.Tag as Document;
        }
        if (doc != null) { CloseDocument(doc); e.Handled = true; }
    }

    // ==================== 快捷键 ====================
    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox) return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var mods = Keyboard.Modifiers;

        var actionId = ShortcutManager.FindAction(key, mods);
        if (actionId == null) return;

        ExecuteShortcut(actionId);
        e.Handled = true;
    }

    private void ExecuteShortcut(string actionId)
    {
        switch (actionId)
        {
            case "Tool_Pen": PenTool.IsChecked = true; SetTool(ToolType.Pen); break;
            case "Tool_Eraser": EraserTool.IsChecked = true; SetTool(ToolType.Eraser); break;
            case "Tool_Fill": FillTool.IsChecked = true; SetTool(ToolType.Fill); break;
            case "Tool_Picker": PickerTool.IsChecked = true; SetTool(ToolType.Picker); break;
            case "Tool_Line": LineTool.IsChecked = true; SetTool(ToolType.Line); break;
            case "Tool_Rect": RectTool.IsChecked = true; SetTool(ToolType.Rect); break;
            case "Tool_Ellipse": EllipseTool.IsChecked = true; SetTool(ToolType.Ellipse); break;

            case "SwapColors": SwapColors_Click(this, new RoutedEventArgs()); break;

            case "BrushSizeDec":
                _brushSize = Math.Max(1, _brushSize - 1);
                if (ToolBarBrushSize != null) ToolBarBrushSize.Value = _brushSize;
                if (StatusLabel != null)
                    StatusLabel.Text = $"{LanguageManager.T("Toolbar_BrushSize")} {_brushSize}";
                break;
            case "BrushSizeInc":
                _brushSize = Math.Min(20, _brushSize + 1);
                if (ToolBarBrushSize != null) ToolBarBrushSize.Value = _brushSize;
                if (StatusLabel != null)
                    StatusLabel.Text = $"{LanguageManager.T("Toolbar_BrushSize")} {_brushSize}";
                break;
            case "OpacityDec":
                _opacity = Math.Max(0, _opacity - 10);
                if (OpacitySlider != null) OpacitySlider.Value = _opacity;
                if (StatusLabel != null)
                    StatusLabel.Text = $"{LanguageManager.T("Toolbar_Opacity")} {_opacity}%";
                break;
            case "OpacityInc":
                _opacity = Math.Min(100, _opacity + 10);
                if (OpacitySlider != null) OpacitySlider.Value = _opacity;
                if (StatusLabel != null)
                    StatusLabel.Text = $"{LanguageManager.T("Toolbar_Opacity")} {_opacity}%";
                break;

            case "Edit_Undo": Undo_Click(this, new RoutedEventArgs()); break;
            case "Edit_Redo": Redo_Click(this, new RoutedEventArgs()); break;

            case "File_New": New_Click(this, new RoutedEventArgs()); break;
            case "File_Open": Open_Click(this, new RoutedEventArgs()); break;
            case "File_Save": Save_Click(this, new RoutedEventArgs()); break;
            case "File_Close": CloseDocument_Click(this, new RoutedEventArgs()); break;

            case "View_ZoomIn": ZoomIn_Click(this, new RoutedEventArgs()); break;
            case "View_ZoomOut": ZoomOut_Click(this, new RoutedEventArgs()); break;
            case "View_ZoomReset": ZoomReset_Click(this, new RoutedEventArgs()); break;
            case "View_NextDoc": NextDoc_Click(this, new RoutedEventArgs()); break;

            case "Layer_New": AddLayer_Click(this, new RoutedEventArgs()); break;
        }
    }

    // ==================== 调色板 ====================
    private void BuildPalette()
    {
        PalettePanel.Children.Clear();
        foreach (var hex in DefaultPalette)
        {
            var btn = new Border
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Background = (Brush)new BrushConverter().ConvertFromString(hex)!,
                Tag = hex,
                ToolTip = hex
            };
            btn.MouseLeftButtonDown += (_, _) => { _foreColor = ParseColor(hex); UpdateColorPreview(); };
            btn.MouseRightButtonDown += (_, _) => { _backColor = ParseColor(hex); UpdateColorPreview(); };
            PalettePanel.Children.Add(btn);
        }
    }

    private void UpdateColorPreview()
    {
        CurrentColorPreview.Background = new SolidColorBrush(_foreColor);
        BgColorSwatch.Background = new SolidColorBrush(_backColor);
        HexColorBox.Text = $"#{_foreColor.R:X2}{_foreColor.G:X2}{_foreColor.B:X2}";
    }

    private static Color ParseColor(string hex)
    {
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
                return Color.FromRgb(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
            if (hex.Length == 8)
                return Color.FromArgb(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16),
                    Convert.ToByte(hex.Substring(6, 2), 16));
        }
        catch { }
        return Colors.Black;
    }

    private void HexColorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _foreColor = ParseColor(HexColorBox.Text);
        CurrentColorPreview.Background = new SolidColorBrush(_foreColor);
    }

    // ==================== 颜色选择器 ====================
    private void CurrentColor_Click(object sender, MouseButtonEventArgs e)
    {
        var dlg = new ColorPickerDialog(_foreColor) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _foreColor = dlg.SelectedColor;
            UpdateColorPreview();
            AddColorToPalette(_foreColor);
        }
    }

    private void BgColor_Click(object sender, MouseButtonEventArgs e)
    {
        var dlg = new ColorPickerDialog(_backColor) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _backColor = dlg.SelectedColor;
            UpdateColorPreview();
            AddColorToPalette(_backColor);
        }
    }

    private void PickColor_Click(object sender, RoutedEventArgs e) => CurrentColor_Click(sender, null!);

    private void SwapColors_Click(object sender, RoutedEventArgs e)
    {
        (_foreColor, _backColor) = (_backColor, _foreColor);
        UpdateColorPreview();
    }

    private void AddColorToPalette(Color c)
    {
        var hex = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        foreach (var child in PalettePanel.Children)
            if (child is Border b && b.Tag is string tag && tag == hex) return;

        var btn = new Border
        {
            Width = 20,
            Height = 20,
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            Cursor = Cursors.Hand,
            Background = new SolidColorBrush(c),
            Tag = hex,
            ToolTip = hex
        };
        btn.MouseLeftButtonDown += (_, _) => { _foreColor = ParseColor(hex); UpdateColorPreview(); };
        PalettePanel.Children.Insert(0, btn);
    }

    // ==================== 滚轮 ====================
    private void CanvasScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            _zoom = e.Delta > 0 ? Math.Min(_zoom * 1.2, 32) : Math.Max(_zoom / 1.2, 0.25);
            ApplyZoom();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            CanvasScroll.ScrollToHorizontalOffset(
                CanvasScroll.HorizontalOffset + (e.Delta > 0 ? -40 : 40));
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Alt)
        {
            int v = Math.Clamp(_brushSize + (e.Delta > 0 ? 1 : -1), 1, 20);
            _brushSize = v;
            ToolBarBrushSize.Value = v;
            e.Handled = true;
        }
    }

    private void ToolBarBrushSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _brushSize = (int)e.NewValue;
        if (ToolBarBrushSizeLabel != null) ToolBarBrushSizeLabel.Text = _brushSize.ToString();
    }

    private void OpacitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _opacity = (int)e.NewValue;
        if (OpacityLabel != null) OpacityLabel.Text = $"{_opacity}%";
    }

    // ==================== 工具 ====================
    private void Tool_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag)
            if (Enum.TryParse<ToolType>(tag, out var tool)) SetTool(tool);
    }

    private void SetTool(ToolType tool)
    {
        _currentTool = tool;
        ToolNameLabel.Text = tool switch
        {
            ToolType.Pen => LanguageManager.T("Tool_Pen"),
            ToolType.Eraser => LanguageManager.T("Tool_Eraser"),
            ToolType.Fill => LanguageManager.T("Tool_Fill"),
            ToolType.Picker => LanguageManager.T("Tool_Picker"),
            ToolType.Line => LanguageManager.T("Tool_Line"),
            ToolType.Rect => LanguageManager.T("Tool_Rect"),
            ToolType.Ellipse => LanguageManager.T("Tool_Ellipse"),
            _ => LanguageManager.T("Tool_Generic")
        };
        if (StatusLabel != null)
            StatusLabel.Text = ToolNameLabel.Text;
    }

    // ==================== 画布交互 ====================
    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Canvas == null) return;
        if (Canvas.ActiveLayer == null)
        {
            if (StatusLabel != null)
                StatusLabel.Text = LanguageManager.T("Msg_SelectLayer");
            return;
        }

        CanvasImage.CaptureMouse();

        var pos = Mouse.GetPosition(CanvasImage);
        int px = (int)(pos.X / _zoom);
        int py = (int)(pos.Y / _zoom);

        bool isRight = e.ChangedButton == MouseButton.Right;
        var drawColor = isRight ? _backColor : ApplyOpacity(_foreColor);

        History.Push("绘制", Canvas.SnapshotActiveLayer());
        _dragStartSnapshot = Canvas.SnapshotActiveLayer();

        _isDrawing = true;
        _startPixel = (px, py);
        _lastPixel = (px, py);

        switch (_currentTool)
        {
            case ToolType.Pen:
                DrawBrush(px, py, drawColor);
                break;
            case ToolType.Eraser:
                DrawBrush(px, py, Colors.Transparent);
                break;
            case ToolType.Fill:
                if (!isRight) Canvas.FloodFill(px, py, ApplyOpacity(_foreColor));
                break;
            case ToolType.Picker:
                {
                    var c = Canvas.GetPixel(px, py);
                    if (c.A > 0) _foreColor = c;
                    UpdateColorPreview();
                    _isDrawing = false;
                    _dragStartSnapshot = null;
                    if (CanvasImage.IsMouseCaptured) CanvasImage.ReleaseMouseCapture();
                }
                break;
        }

        ForceRefresh();
    }

    private Color ApplyOpacity(Color c)
    {
        byte a = (byte)(c.A * _opacity / 100);
        return Color.FromArgb(a, c.R, c.G, c.B);
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (Canvas == null) return;

        var pos = Mouse.GetPosition(CanvasImage);
        int px = (int)(pos.X / _zoom);
        int py = (int)(pos.Y / _zoom);

        UpdateBrushCursor(pos, px, py, Canvas.Width, Canvas.Height);

        if (!_isDrawing)
        {
            if (px >= 0 && py >= 0 && px < Canvas.Width && py < Canvas.Height)
                StatusLabel.Text = $"{px}, {py}";
            return;
        }

        int x = px;
        int y = py;

        bool isRight = e.RightButton == MouseButtonState.Pressed;
        var drawColor = isRight ? _backColor : ApplyOpacity(_foreColor);

        switch (_currentTool)
        {
            case ToolType.Pen:
                DrawLineBetween(_lastPixel, (x, y), drawColor);
                break;
            case ToolType.Eraser:
                DrawLineBetween(_lastPixel, (x, y), Colors.Transparent);
                break;

            case ToolType.Line:
            case ToolType.Rect:
            case ToolType.Ellipse:
                if (_dragStartSnapshot != null)
                    Canvas.RestoreActiveLayer(_dragStartSnapshot);

                switch (_currentTool)
                {
                    case ToolType.Line:
                        DrawLineWithMirror(_startPixel.x, _startPixel.y, x, y, drawColor);
                        break;
                    case ToolType.Rect:
                        DrawRectWithMirror(_startPixel.x, _startPixel.y, x, y, drawColor);
                        break;
                    case ToolType.Ellipse:
                        DrawEllipseWithMirror(_startPixel.x, _startPixel.y, x, y, drawColor);
                        break;
                }
                break;
        }

        _lastPixel = (x, y);
        RequestRefresh();
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (CanvasImage.IsMouseCaptured)
            CanvasImage.ReleaseMouseCapture();

        if (!_isDrawing || Canvas == null)
        {
            _dragStartSnapshot = null;
            return;
        }
        _isDrawing = false;

        var p = Mouse.GetPosition(CanvasImage);
        int x = (int)(p.X / _zoom);
        int y = (int)(p.Y / _zoom);
        bool isRight = e.ChangedButton == MouseButton.Right;
        var drawColor = isRight ? _backColor : ApplyOpacity(_foreColor);

        if (_dragStartSnapshot != null &&
            (_currentTool == ToolType.Line ||
             _currentTool == ToolType.Rect ||
             _currentTool == ToolType.Ellipse))
        {
            Canvas.RestoreActiveLayer(_dragStartSnapshot);
        }

        switch (_currentTool)
        {
            case ToolType.Line:
                DrawLineWithMirror(_startPixel.x, _startPixel.y, x, y, drawColor);
                break;
            case ToolType.Rect:
                DrawRectWithMirror(_startPixel.x, _startPixel.y, x, y, drawColor);
                break;
            case ToolType.Ellipse:
                DrawEllipseWithMirror(_startPixel.x, _startPixel.y, x, y, drawColor);
                break;
        }

        _dragStartSnapshot = null;

        ForceRefresh();
        RefreshHistoryList();
        RefreshLayerTree();
    }

    private void Canvas_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!CanvasImage.IsMouseCaptured) _isDrawing = false;
        HideBrushCursor();
    }

    private void CanvasImage_LostMouseCapture(object sender, MouseEventArgs e) => _isDrawing = false;

    private void DrawBrush(int x, int y, Color c)
    {
        if (Canvas == null) return;

        if (_brushSize <= 1)
        {
            SetPixelWithMirror(x, y, c);
            return;
        }

        int half = _brushSize / 2;
        int r2 = half * half + 1;
        for (int dy = -half; dy <= half; dy++)
            for (int dx = -half; dx <= half; dx++)
                if (dx * dx + dy * dy <= r2)
                    SetPixelWithMirror(x + dx, y + dy, c);
    }

    private void DrawLineBetween((int x, int y) a, (int x, int y) b, Color c)
    {
        int dx = Math.Abs(b.x - a.x), sx = a.x < b.x ? 1 : -1;
        int dy = -Math.Abs(b.y - a.y), sy = a.y < b.y ? 1 : -1;
        int err = dx + dy;
        int x0 = a.x, y0 = a.y;

        while (true)
        {
            DrawBrush(x0, y0, c);
            if (x0 == b.x && y0 == b.y) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private void DrawLineWithMirror(int x0, int y0, int x1, int y1, Color c)
    {
        if (Canvas == null) return;
        int w = Canvas.Width, h = Canvas.Height;

        Canvas.DrawLine(x0, y0, x1, y1, c);

        if (_mirrorMode == MirrorMode.Horizontal || _mirrorMode == MirrorMode.Quadrant)
            Canvas.DrawLine(w - 1 - x0, y0, w - 1 - x1, y1, c);

        if (_mirrorMode == MirrorMode.Vertical || _mirrorMode == MirrorMode.Quadrant)
            Canvas.DrawLine(x0, h - 1 - y0, x1, h - 1 - y1, c);

        if (_mirrorMode == MirrorMode.Quadrant)
            Canvas.DrawLine(w - 1 - x0, h - 1 - y0, w - 1 - x1, h - 1 - y1, c);
    }

    private void DrawRectWithMirror(int x0, int y0, int x1, int y1, Color c)
    {
        if (Canvas == null) return;
        int w = Canvas.Width, h = Canvas.Height;

        Canvas.DrawRect(x0, y0, x1, y1, c, false);

        if (_mirrorMode == MirrorMode.Horizontal || _mirrorMode == MirrorMode.Quadrant)
            Canvas.DrawRect(w - 1 - x0, y0, w - 1 - x1, y1, c, false);

        if (_mirrorMode == MirrorMode.Vertical || _mirrorMode == MirrorMode.Quadrant)
            Canvas.DrawRect(x0, h - 1 - y0, x1, h - 1 - y1, c, false);

        if (_mirrorMode == MirrorMode.Quadrant)
            Canvas.DrawRect(w - 1 - x0, h - 1 - y0, w - 1 - x1, h - 1 - y1, c, false);
    }

    private void DrawEllipseWithMirror(int x0, int y0, int x1, int y1, Color c)
    {
        if (Canvas == null) return;
        int w = Canvas.Width, h = Canvas.Height;

        Canvas.DrawEllipse(x0, y0, x1, y1, c);

        if (_mirrorMode == MirrorMode.Horizontal || _mirrorMode == MirrorMode.Quadrant)
            Canvas.DrawEllipse(w - 1 - x0, y0, w - 1 - x1, y1, c);

        if (_mirrorMode == MirrorMode.Vertical || _mirrorMode == MirrorMode.Quadrant)
            Canvas.DrawEllipse(x0, h - 1 - y0, x1, h - 1 - y1, c);

        if (_mirrorMode == MirrorMode.Quadrant)
            Canvas.DrawEllipse(w - 1 - x0, h - 1 - y0, w - 1 - x1, h - 1 - y1, c);
    }

    // ==================== 文件菜单 ====================
    private void New_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NewCanvasDialog { Owner = this };
        if (dlg.ShowDialog() == true)
            NewDocument(dlg.CanvasWidth, dlg.CanvasHeight, dlg.BackgroundColor);
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "PNG 图片|*.png|所有文件|*.*" };
        if (dlg.ShowDialog() != true) return;
        OpenFileByPath(dlg.FileName);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        if (string.IsNullOrEmpty(_current.FilePath)) { SaveAs_Click(sender, e); return; }
        SaveToFile(_current.FilePath);
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;

        // 把所有的格式写在弹窗的下拉列表里，让用户自己选
        var dlg = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png|JPEG 图片|*.jpg|BMP 图片|*.bmp|ICO 图标|*.ico",
            FilterIndex = 1,
            DefaultExt = "png",
            FileName = _current.Title
        };

        if (dlg.ShowDialog() != true) return;

        // 根据用户选的后缀名，自动推断格式
        var format = Path.GetExtension(dlg.FileName).TrimStart('.').ToLower();
        SaveToFile(dlg.FileName, format);
    }

    private void SaveToFile(string path, string format = "")
    {
        if (_current == null) return;
        if (string.IsNullOrEmpty(format)) format = Path.GetExtension(path).TrimStart('.').ToLower();

        try
        {
            _current.Canvas.Refresh();
            BitmapEncoder encoder = format switch
            {
                "jpg" => new JpegBitmapEncoder(),
                "bmp" => new BmpBitmapEncoder(),
                _ => new PngBitmapEncoder()
            };
            encoder.Frames.Add(BitmapFrame.Create(_current.Canvas.Bitmap));
            using var fs = File.Create(path);
            encoder.Save(fs);
            _current.FilePath = path;

            RecentFilesManager.Add(path);

            StatusLabel.Text = string.Format(
                LanguageManager.T("Msg_Saved"), Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(LanguageManager.T("Msg_SaveFailed"), ex.Message),
                LanguageManager.T("Msg_ErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CloseDocument_Click(object sender, RoutedEventArgs e)
    {
        if (_current != null) CloseDocument(_current);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    // ==================== 编辑 ====================
    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas == null) return;
        var prev = History.Undo();
        if (prev == null) return;
        Canvas.RestoreActiveLayer(prev);
        ForceRefresh();
        RefreshHistoryList();
        RefreshLayerTree();
        UpdateUndoRedoState();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas == null) return;
        var next = History.Redo();
        if (next == null) return;
        Canvas.RestoreActiveLayer(next);
        ForceRefresh();
        RefreshHistoryList();
        RefreshLayerTree();
        UpdateUndoRedoState();
    }

    private void ClearLayer_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas == null || Canvas.ActiveLayer == null) return;
        History.Push("清空", Canvas.SnapshotActiveLayer());
        Canvas.ActiveLayer.Clear();
        ForceRefresh();
        RefreshHistoryList();
        RefreshLayerTree();
    }

    private void OnHistoryChanged() { Dispatcher.Invoke(() => { RefreshHistoryList(); UpdateUndoRedoState(); }); }

    private void UpdateUndoRedoState()
    {
        if (UndoMenuItem != null) UndoMenuItem.IsEnabled = History.CanUndo;
        if (RedoMenuItem != null) RedoMenuItem.IsEnabled = History.CanRedo;
    }

    // ==================== 图像菜单 ====================
    private void CanvasSize_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            LanguageManager.T("Msg_CanvasSizeHint"),
            LanguageManager.T("Msg_InfoTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void FlipH_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas?.ActiveLayer == null) return;
        var layer = Canvas.ActiveLayer;
        int w = Canvas.Width, h = Canvas.Height;
        var copy = layer.ClonePixels();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int srcIdx = (y * w + (w - 1 - x)) * 4;
                int dstIdx = (y * w + x) * 4;
                layer.Pixels[dstIdx] = copy[srcIdx];
                layer.Pixels[dstIdx + 1] = copy[srcIdx + 1];
                layer.Pixels[dstIdx + 2] = copy[srcIdx + 2];
                layer.Pixels[dstIdx + 3] = copy[srcIdx + 3];
            }
        ForceRefresh();
        RefreshLayerTree();
    }

    private void FlipV_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas?.ActiveLayer == null) return;
        var layer = Canvas.ActiveLayer;
        int w = Canvas.Width, h = Canvas.Height;
        var copy = layer.ClonePixels();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int srcIdx = ((h - 1 - y) * w + x) * 4;
                int dstIdx = (y * w + x) * 4;
                layer.Pixels[dstIdx] = copy[srcIdx];
                layer.Pixels[dstIdx + 1] = copy[srcIdx + 1];
                layer.Pixels[dstIdx + 2] = copy[srcIdx + 2];
                layer.Pixels[dstIdx + 3] = copy[srcIdx + 3];
            }
        ForceRefresh();
        RefreshLayerTree();
    }

    // ==================== 图层菜单 ====================
    private void AddLayer_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas == null) return;
        History.Push("新建图层", Canvas.SnapshotActiveLayer());
        Canvas.AddLayer();
        RefreshLayerTree();
        ForceRefresh();
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas == null) return;
        Canvas.AddFolder();
        RefreshLayerTree();
    }

    private void RemoveLayer_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas == null) return;
        if (Canvas.ActiveNode != null && Canvas.ActiveNode != Canvas.Root)
        {
            Canvas.RemoveNode(Canvas.ActiveNode);
            RefreshLayerTree();
            ForceRefresh();
        }
    }

    private void MoveLayerUp_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas?.ActiveNode == null) return;
        MoveLayerNodeUp(Canvas.ActiveNode);
    }

    private void MoveLayerDown_Click(object sender, RoutedEventArgs e)
    {
        if (Canvas?.ActiveNode == null) return;
        MoveLayerNodeDown(Canvas.ActiveNode);
    }

    // ==================== 视图菜单 ====================
    private void ZoomIn_Click(object sender, RoutedEventArgs e) { _zoom = Math.Min(_zoom * 2, 32); ApplyZoom(); }
    private void ZoomOut_Click(object sender, RoutedEventArgs e) { _zoom = Math.Max(_zoom / 2, 0.25); ApplyZoom(); }
    private void ZoomReset_Click(object sender, RoutedEventArgs e) { _zoom = 1.0; ApplyZoom(); }
    private void ToggleGrid_Click(object sender, RoutedEventArgs e) { _showGrid = GridMenuItem.IsChecked; SetupGrid(); }

    private void ApplyZoom()
    {
        UpdateCanvasSize();
        UpdateZoomDisplay();
        SetupGrid();
    }

    private void UpdateCanvasSize()
    {
        if (Canvas == null) return;
        CanvasImage.Width = Canvas.Width * _zoom;
        CanvasImage.Height = Canvas.Height * _zoom;
    }

    private void UpdateZoomDisplay() { ZoomLabel.Text = $"{(_zoom * 100):F0}%"; }

    private void UpdateDocInfo()
    {
        if (Canvas == null) { DocInfoLabel.Text = ""; return; }
        DocInfoLabel.Text = $"{Canvas.Width} × {Canvas.Height}";
    }

    private void SetupGrid()
    {
        if (GridOverlay == null || Canvas == null) return;

        if (!_showGrid || _zoom < 4)
        {
            GridOverlay.Background = null;
            GridOverlay.Width = 0;
            GridOverlay.Height = 0;
            return;
        }

        GridOverlay.Width = Canvas.Width * _zoom;
        GridOverlay.Height = Canvas.Height * _zoom;

        double cell = _zoom;

        var pen = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 1)
        {
            StartLineCap = PenLineCap.Square,
            EndLineCap = PenLineCap.Square
        };

        var geometry = new GeometryGroup();
        geometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(cell, 0)));
        geometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, cell)));

        var gridBrush = new DrawingBrush
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, cell, cell),
            ViewportUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0, 0, cell, cell),
            ViewboxUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            Drawing = new GeometryDrawing { Pen = pen, Geometry = geometry }
        };

        GridOverlay.Background = gridBrush;
    }

    // ==================== 图层树 ====================
    private void RefreshLayerTree()
    {
        _updatingLayerTree = true;
        LayerTree.Items.Clear();
        if (Canvas != null)
        {
            for (int i = Canvas.Root.Children.Count - 1; i >= 0; i--)
                LayerTree.Items.Add(BuildTreeViewItem(Canvas.Root.Children[i]));
        }
        _updatingLayerTree = false;
    }

    private TreeViewItem BuildTreeViewItem(LayerNode node)
    {
        var item = new TreeViewItem
        {
            Tag = node,
            IsExpanded = node.IsExpanded,
            Header = BuildNodeHeader(node),
            ContextMenu = BuildLayerContextMenu(node)
        };

        item.PreviewMouseRightButtonDown += (_, _) =>
        {
            if (!item.IsSelected) item.IsSelected = true;
        };

        for (int i = node.Children.Count - 1; i >= 0; i--)
            item.Items.Add(BuildTreeViewItem(node.Children[i]));

        if (node == Canvas?.ActiveNode)
            item.IsSelected = true;

        item.Expanded += (_, _) => node.IsExpanded = true;
        item.Collapsed += (_, _) => node.IsExpanded = false;

        return item;
    }

    private ContextMenu BuildLayerContextMenu(LayerNode node)
    {
        var menu = new ContextMenu
        {
            Style = (Style)FindResource("LayerContextMenuStyle")
        };

        menu.Resources[typeof(MenuItem)] = FindResource("LayerMenuItemStyle");
        menu.Resources[typeof(Separator)] = FindResource("LayerSeparatorStyle");

        menu.Opened += LayerMenu_Opened;
        menu.Closed += LayerMenu_Closed;

        var renameItem = new MenuItem { Header = LanguageManager.T("Ctx_Rename") };
        renameItem.Click += (_, _) => RenameLayerNode(node);
        menu.Items.Add(renameItem);

        var deleteItem = new MenuItem { Header = LanguageManager.T("Ctx_Delete") };
        deleteItem.Click += (_, _) => DeleteLayerNode(node);
        if (node == Canvas?.Root) deleteItem.IsEnabled = false;
        menu.Items.Add(deleteItem);

        menu.Items.Add(new Separator());

        if (node.IsLayer)
        {
            var addLayerItem = new MenuItem { Header = LanguageManager.T("Layer_New") };
            addLayerItem.Click += (_, _) => AddLayerNearNode(node);
            menu.Items.Add(addLayerItem);
        }
        else
        {
            var addLayerItem = new MenuItem { Header = LanguageManager.T("Ctx_AddLayerHere") };
            addLayerItem.Click += (_, _) => AddLayerNearNode(node);
            menu.Items.Add(addLayerItem);

            var addFolderItem = new MenuItem { Header = LanguageManager.T("Ctx_AddFolderHere") };
            addFolderItem.Click += (_, _) => AddFolderNearNode(node);
            menu.Items.Add(addFolderItem);
        }

        menu.Items.Add(new Separator());

        var moveUpItem = new MenuItem { Header = LanguageManager.T("Layer_MoveUp") };
        moveUpItem.Click += (_, _) => MoveLayerNodeUp(node);
        menu.Items.Add(moveUpItem);

        var moveDownItem = new MenuItem { Header = LanguageManager.T("Layer_MoveDown") };
        moveDownItem.Click += (_, _) => MoveLayerNodeDown(node);
        menu.Items.Add(moveDownItem);

        return menu;
    }

    private void LayerMenu_Opened(object? sender, RoutedEventArgs e)
    {
        if (LayerTree.Effect == null)
            LayerTree.Effect = _menuBackdropBlur;
    }

    private void LayerMenu_Closed(object? sender, RoutedEventArgs e)
    {
        LayerTree.Effect = null;
    }

    private void RenameLayerNode(LayerNode node)
    {
        var dlg = new RenameDialog(node.Name) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            node.Name = dlg.NewName;
            if (node.IsLayer && node.Layer != null) node.Layer.Name = dlg.NewName;
            RefreshLayerTree();
        }
    }

    private void DeleteLayerNode(LayerNode node)
    {
        if (Canvas == null || node == Canvas.Root) return;
        Canvas.RemoveNode(node);
        RefreshLayerTree();
        ForceRefresh();
    }

    private void AddLayerNearNode(LayerNode node)
    {
        if (Canvas == null) return;
        if (node.IsLayer) Canvas.ActiveNode = node;
        History.Push("新建图层", Canvas.SnapshotActiveLayer());
        Canvas.AddLayer();
        RefreshLayerTree();
        ForceRefresh();
    }

    private void AddFolderNearNode(LayerNode node)
    {
        if (Canvas == null) return;
        if (node.IsLayer) Canvas.ActiveNode = node;
        Canvas.AddFolder();
        RefreshLayerTree();
    }

    private void MoveLayerNodeUp(LayerNode node)
    {
        if (node.Parent == null) return;
        var siblings = node.Parent.Children;
        int idx = siblings.IndexOf(node);
        if (idx >= 0 && idx < siblings.Count - 1)
        {
            (siblings[idx], siblings[idx + 1]) = (siblings[idx + 1], siblings[idx]);
            RefreshLayerTree();
            ForceRefresh();
        }
    }

    private void MoveLayerNodeDown(LayerNode node)
    {
        if (node.Parent == null) return;
        var siblings = node.Parent.Children;
        int idx = siblings.IndexOf(node);
        if (idx > 0)
        {
            (siblings[idx], siblings[idx - 1]) = (siblings[idx - 1], siblings[idx]);
            RefreshLayerTree();
            ForceRefresh();
        }
    }

    private UIElement BuildNodeHeader(LayerNode node)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var eyeBorder = new Border
        {
            Width = 22,
            Height = 20,
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };

        var eyeText = new TextBlock
        {
            Text = node.Visible ? "👁" : "🚫",
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(node.Visible ? Colors.White : Color.FromRgb(0x6E, 0x6E, 0x6E)),
            IsHitTestVisible = false
        };
        eyeBorder.Child = eyeText;

        eyeBorder.PreviewMouseLeftButtonDown += (_, ev) =>
        {
            _dragSourceNode = null;

            node.Visible = !node.Visible;
            eyeText.Text = node.Visible ? "👁" : "🚫";
            eyeText.Foreground = new SolidColorBrush(
                node.Visible ? Colors.White : Color.FromRgb(0x6E, 0x6E, 0x6E));
            ev.Handled = true;
            Dispatcher.BeginInvoke(new Action(() => ForceRefresh()), DispatcherPriority.Render);
        };

        Grid.SetColumn(eyeBorder, 0);
        grid.Children.Add(eyeBorder);

        if (node.IsFolder)
        {
            var folderIcon = new TextBlock
            {
                Text = "📁",
                FontSize = 13,
                Margin = new Thickness(2, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            Grid.SetColumn(folderIcon, 1);
            grid.Children.Add(folderIcon);
        }
        else if (node.IsLayer && node.Layer != null)
        {
            var thumb = CreateThumbnailImage(node.Layer);
            Grid.SetColumn(thumb, 1);
            grid.Children.Add(thumb);
        }

        var name = new TextBlock
        {
            Text = node.Name,
            Foreground = node.IsFolder ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x82)) : Brushes.White,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            IsHitTestVisible = false
        };
        Grid.SetColumn(name, 2);
        grid.Children.Add(name);

        return grid;
    }

    private void LayerTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_updatingLayerTree || Canvas == null) return;
        if (e.NewValue is TreeViewItem item && item.Tag is LayerNode node)
        {
            if (node.IsLayer) Canvas.ActiveNode = node;
        }
    }

    private void LayerTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Canvas == null) return;
        if (LayerTree.SelectedItem is TreeViewItem item && item.Tag is LayerNode node)
            RenameLayerNode(node);
    }

    // ==================== 图层拖拽 ====================
    private enum DropPosition { None, Before, After, Inside }

    private void LayerTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);
        _dragSourceNode = item?.Tag as LayerNode;
        _dragStartPoint = e.GetPosition(LayerTree);
        _isDraggingLayer = false;
        _currentDropTarget = null;
        _currentDropPos = DropPosition.None;
    }

    private void LayerTree_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            if (_isDraggingLayer) EndLayerDrag();
            return;
        }

        if (_dragSourceNode == null || Canvas == null) return;

        var pos = e.GetPosition(LayerTree);

        if (!_isDraggingLayer)
        {
            if (Math.Abs(pos.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pos.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            _isDraggingLayer = true;
            LayerTree.CaptureMouse();
        }

        var item = GetItemUnderMouse(pos);
        if (item == null || item.Tag is not LayerNode target)
        {
            HideDropIndicators();
            _currentDropTarget = null;
            _currentDropPos = DropPosition.None;
            return;
        }

        var dropPos = CalcDropPosition(item, pos, target);

        if (!IsValidDrop(_dragSourceNode, target, dropPos))
        {
            HideDropIndicators();
            _currentDropTarget = null;
            _currentDropPos = DropPosition.None;
            return;
        }

        ShowDropIndicator(item, dropPos);
        _currentDropTarget = target;
        _currentDropPos = dropPos;
    }

    private void LayerTree_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingLayer) return;

        var source = _dragSourceNode;
        var target = _currentDropTarget;
        var pos = _currentDropPos;

        EndLayerDrag();

        if (source != null && target != null && pos != DropPosition.None)
        {
            PerformDrop(source, target, pos);
        }

        e.Handled = true;
    }

    private void LayerTree_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_isDraggingLayer)
            EndLayerDrag();
    }

    private void EndLayerDrag()
    {
        _isDraggingLayer = false;
        _dragSourceNode = null;
        _currentDropTarget = null;
        _currentDropPos = DropPosition.None;
        HideDropIndicators();
        if (LayerTree.IsMouseCaptured) LayerTree.ReleaseMouseCapture();
    }

    private TreeViewItem? GetItemUnderMouse(Point ptInTree)
    {
        var all = new List<TreeViewItem>();
        CollectTreeItems(LayerTree, all);

        TreeViewItem? best = null;
        double bestDistance = double.MaxValue;

        foreach (var item in all)
        {
            var header = GetHeaderElement(item);
            if (header == null) continue;

            double top, bottom;
            try
            {
                var transform = header.TransformToAncestor(LayerTree);
                var topLeft = transform.Transform(new Point(0, 0));
                top = topLeft.Y;
                bottom = top + header.ActualHeight;
            }
            catch
            {
                continue;
            }

            if (ptInTree.Y >= top && ptInTree.Y <= bottom)
                return item;

            double dist = ptInTree.Y < top ? top - ptInTree.Y : ptInTree.Y - bottom;
            if (dist < bestDistance)
            {
                bestDistance = dist;
                best = item;
            }
        }

        if (bestDistance > 20) return null;
        return best;
    }

    private static void CollectTreeItems(ItemsControl parent, List<TreeViewItem> list)
    {
        for (int i = 0; i < parent.Items.Count; i++)
        {
            if (parent.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem item)
            {
                list.Add(item);
                if (item.IsExpanded)
                    CollectTreeItems(item, list);
            }
        }
    }

    private FrameworkElement? GetHeaderElement(TreeViewItem item)
    {
        if (item.Template != null)
        {
            var header = item.Template.FindName("PART_Header", item) as FrameworkElement;
            if (header != null && header.ActualHeight > 0) return header;
        }

        int count = VisualTreeHelper.GetChildrenCount(item);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(item, i);
            if (child is ContentPresenter cp && cp.ActualHeight > 0) return cp;
            int sub = VisualTreeHelper.GetChildrenCount(child);
            for (int j = 0; j < sub; j++)
            {
                var inner = VisualTreeHelper.GetChild(child, j);
                if (inner is ContentPresenter cp2 && cp2.ActualHeight > 0) return cp2;
            }
        }
        return null;
    }

    private DropPosition CalcDropPosition(TreeViewItem item, Point ptInTree, LayerNode target)
    {
        var header = GetHeaderElement(item);
        if (header == null) return DropPosition.After;

        double top;
        try
        {
            var transform = header.TransformToAncestor(LayerTree);
            top = transform.Transform(new Point(0, 0)).Y;
        }
        catch { return DropPosition.After; }

        double h = Math.Max(header.ActualHeight, 1);
        double ratio = (ptInTree.Y - top) / h;

        if (ratio < 0.30) return DropPosition.Before;
        if (ratio > 0.70) return DropPosition.After;

        if (target.IsFolder) return DropPosition.Inside;
        return ratio < 0.50 ? DropPosition.Before : DropPosition.After;
    }

    private bool IsValidDrop(LayerNode source, LayerNode target, DropPosition pos)
    {
        if (Canvas == null) return false;
        if (source == target) return false;
        if (source == Canvas.Root || target == Canvas.Root) return false;

        var p = target;
        while (p != null)
        {
            if (p == source) return false;
            p = p.Parent;
        }
        return true;
    }

    private void ShowDropIndicator(TreeViewItem item, DropPosition pos)
    {
        var header = GetHeaderElement(item);
        FrameworkElement refElem = header ?? item;

        double x, y, w, h;
        try
        {
            var transform = refElem.TransformToAncestor(LayerTree);
            var topLeft = transform.Transform(new Point(0, 0));
            x = Math.Max(0, topLeft.X);
            y = topLeft.Y;
            w = Math.Max(0, refElem.ActualWidth);
            h = refElem.ActualHeight;
        }
        catch
        {
            HideDropIndicators();
            return;
        }

        if (pos == DropPosition.Before)
        {
            DropLine.Margin = new Thickness(x, y - 1, 0, 0);
            DropLine.Width = w;
            DropLine.Visibility = Visibility.Visible;
            DropHighlight.Visibility = Visibility.Collapsed;
        }
        else if (pos == DropPosition.After)
        {
            DropLine.Margin = new Thickness(x, y + h - 1, 0, 0);
            DropLine.Width = w;
            DropLine.Visibility = Visibility.Visible;
            DropHighlight.Visibility = Visibility.Collapsed;
        }
        else
        {
            DropHighlight.Margin = new Thickness(x, y, 0, 0);
            DropHighlight.Width = w;
            DropHighlight.Height = h;
            DropHighlight.Visibility = Visibility.Visible;
            DropLine.Visibility = Visibility.Collapsed;
        }
    }

    private void HideDropIndicators()
    {
        DropLine.Visibility = Visibility.Collapsed;
        DropHighlight.Visibility = Visibility.Collapsed;
    }

    private void PerformDrop(LayerNode source, LayerNode target, DropPosition pos)
    {
        if (Canvas == null || source.Parent == null) return;

        if (pos == DropPosition.Inside)
        {
            source.AttachTo(target);
            target.IsExpanded = true;
        }
        else
        {
            var newParent = target.Parent ?? Canvas.Root;

            int targetIdx = newParent.Children.IndexOf(target);
            if (targetIdx < 0) targetIdx = newParent.Children.Count;

            // ★ 视觉的 After/Before 和数据的 After/Before 是反的（因为显示是倒序）
            int insertIdx = pos == DropPosition.Before ? targetIdx + 1 : targetIdx;

            if (source.Parent == newParent)
            {
                int sourceIdx = newParent.Children.IndexOf(source);
                if (sourceIdx >= 0 && sourceIdx < insertIdx)
                    insertIdx--;
            }

            source.AttachTo(newParent, insertIdx);
        }

        RefreshLayerTree();
        ForceRefresh();

        StatusLabel.Text = pos == DropPosition.Inside
            ? string.Format(LanguageManager.T("Status_MovedInto"), target.Name)
            : string.Format(
                LanguageManager.T("Status_MovedTo"),
                target.Name,
                LanguageManager.T(pos == DropPosition.Before ? "Status_Before" : "Status_After"));
    }

    private static T? FindAncestor<T>(DependencyObject? obj) where T : DependencyObject
    {
        while (obj != null)
        {
            if (obj is T t) return t;
            obj = obj is Visual || obj is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(obj)
                : LogicalTreeHelper.GetParent(obj);
        }
        return null;
    }

    // ==================== 历史记录 ====================
    private void RefreshHistoryList()
    {
        _updatingHistoryList = true;
        HistoryList.Items.Clear();
        var entries = History.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var item = new ListBoxItem
            {
                Content = $"{i + 1}. {entries[i].Name}",
                Tag = i,
                Foreground = i == History.CurrentIndex
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A))
            };
            HistoryList.Items.Add(item);
        }
        if (History.CurrentIndex >= 0 && History.CurrentIndex < HistoryList.Items.Count)
            HistoryList.SelectedIndex = History.CurrentIndex;
        _updatingHistoryList = false;
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingHistoryList || Canvas == null) return;
        if (HistoryList.SelectedItem is ListBoxItem item && item.Tag is int idx)
        {
            var snapshot = History.JumpTo(idx);
            if (snapshot != null)
            {
                Canvas.RestoreActiveLayer(snapshot);
                ForceRefresh();
                RefreshLayerTree();
                UpdateUndoRedoState();
            }
        }
    }

    // ==================== 窗口菜单 ====================
    private void NextDoc_Click(object sender, RoutedEventArgs e)
    {
        if (_documents.Count <= 1) return;
        int idx = _documents.IndexOf(_current!);
        idx = (idx + 1) % _documents.Count;
        SwitchDocument(_documents[idx]);
    }

    // ==================== 设置 ====================
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow { Owner = this };
        var result = win.ShowDialog();

        if (result == true)
        {
            var s = SettingsManager.Current;

            _showGrid = s.DefaultShowGrid;
            if (GridMenuItem != null) GridMenuItem.IsChecked = _showGrid;
            SetupGrid();

            _brushSize = Math.Clamp(s.DefaultBrushSize, 1, 20);
            if (ToolBarBrushSize != null) ToolBarBrushSize.Value = _brushSize;

            _opacity = Math.Clamp(s.DefaultOpacity, 0, 100);
            if (OpacitySlider != null) OpacitySlider.Value = _opacity;

            StatusLabel.Text = LanguageManager.T("Status_SettingsSaved");
        }
    }

    // ==================== 帮助 ====================
    private void ShowHelp_Click(object sender, RoutedEventArgs e)
    {
        var win = new HelpWindow { Owner = this };
        win.ShowDialog();
    }

    private void ShowAbout_Click(object sender, RoutedEventArgs e)
    {
        var win = new AboutWindow { Owner = this };
        win.ShowDialog();
    }
}

public enum ToolType
{
    Pen, Eraser, Fill, Picker, Line, Rect, Ellipse
}