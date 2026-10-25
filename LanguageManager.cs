using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PixelStudio;

public static class LanguageManager
{
    public static string Current { get; private set; } = "zh-CN";

    private static readonly Dictionary<string, Dictionary<string, string>> BuiltIn = new();
    private static readonly Dictionary<string, Dictionary<string, string>> External = new();
    private static readonly Dictionary<string, string> DisplayNames = new();

    public static (string Code, string Display)[] Available
    {
        get
        {
            var list = new List<(string, string)>();
            foreach (var code in BuiltIn.Keys)
                list.Add((code, DisplayNames.TryGetValue(code, out var n) ? n : code));
            foreach (var code in External.Keys)
                list.Add((code, DisplayNames.TryGetValue(code, out var n) ? n : code));
            return list.ToArray();
        }
    }

    static LanguageManager()
    {
        BuiltIn["zh-CN"] = ZhDict;
        BuiltIn["en-US"] = EnDict;
        DisplayNames["zh-CN"] = "简体中文";
        DisplayNames["en-US"] = "English";
    }

    public static void LoadExternalPacks()
    {
        External.Clear();

        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var dirs = new List<string>
        {
            Path.Combine(exeDir, "Languages"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PixelStudio", "Languages")
        };

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        foreach (var dir in dirs)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    if (dir.StartsWith(exeDir, StringComparison.OrdinalIgnoreCase))
                        Directory.CreateDirectory(dir);
                    continue;
                }

                var files = Directory.GetFiles(dir, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var pack = JsonSerializer.Deserialize<LanguagePack>(json, options);

                        if (pack == null) continue;
                        if (string.IsNullOrWhiteSpace(pack.Code)) continue;
                        if (pack.Strings == null || pack.Strings.Count == 0) continue;
                        if (BuiltIn.ContainsKey(pack.Code)) continue;

                        if (External.ContainsKey(pack.Code)) continue;

                        External[pack.Code] = pack.Strings;
                        DisplayNames[pack.Code] =
                            string.IsNullOrWhiteSpace(pack.Name) ? pack.Code : pack.Name;
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    public static string T(string key)
    {
        if (External.TryGetValue(Current, out var ext) &&
            ext.TryGetValue(key, out var v1))
            return v1;

        if (BuiltIn.TryGetValue(Current, out var bi) &&
            bi.TryGetValue(key, out var v2))
            return v2;

        if (BuiltIn.TryGetValue("zh-CN", out var zh) &&
            zh.TryGetValue(key, out var v3))
            return v3;

        return key;
    }

    public static void SetLanguage(string code)
    {
        Current = code;
    }

    private static readonly Dictionary<string, string> ZhDict = new()
    {
        ["Common_OK"] = "确定",
        ["Common_Cancel"] = "取消",
        ["Common_Save"] = "保存",
        ["Common_Close"] = "关闭",
        ["Common_Reset"] = "恢复默认",
        ["Common_Confirm"] = "确认",

        ["Settings_Title"] = "设置",
        ["Settings_General"] = "通用",
        ["Settings_Performance"] = "性能",
        ["Settings_Shortcuts"] = "快捷键",
        ["Settings_Plugins"] = "插件",
        ["Settings_Thanks"] = "致谢",
        ["Settings_About"] = "关于",
        ["Settings_Personalization"] = "个性化",
        ["Settings_Language"] = "语言",
        ["Settings_LanguageHint"] = "切换语言后需要重启软件生效",
        ["Settings_ShowGrid"] = "新文档默认显示网格",
        ["Settings_BrushSize"] = "新文档默认笔刷大小",
        ["Settings_Opacity"] = "新文档默认不透明度",
        ["Settings_HistoryBudget"] = "历史记录内存预算",
        ["Settings_HistoryHint"] = "限制的是历史快照的总大小。软件整体内存还包括画布像素和 WPF 框架开销。",
        ["Settings_MemoryEstimate"] = "估算可撤销步数（上限 60）：",
        ["Settings_Shortcut_Brush"] = "画笔 / 橡皮",
        ["Settings_Shortcut_Fill"] = "油漆桶 / 吸管",
        ["Settings_Shortcut_Shape"] = "直线 / 矩形",
        ["Settings_Shortcut_Ellipse"] = "椭圆",
        ["Settings_Shortcut_Undo"] = "撤销 / 重做",
        ["Settings_Shortcut_Zoom"] = "缩放",
        ["Settings_Shortcut_Tab"] = "切换文档",
        ["Settings_ThanksHint"] = "感谢所有帮助过这个项目的朋友。",
        ["Settings_ViewThanks"] = "查看致谢名单 →",
        ["Settings_Version"] = "ZeroHush v1.0.0-beta",
        ["Settings_Tagline"] = "轻量像素画工具 · 开源免费",
        ["Settings_BuiltWith"] = "基于 .NET 10 + WPF 构建",

        ["Plugins_None"] = "还没有安装任何插件",
        ["Plugins_Hint"] = "把插件 DLL 文件放到 Plugins 文件夹，重启软件即可加载。",
        ["Plugins_Folder"] = "打开 Plugins 文件夹",
        ["Plugins_Version"] = "版本",
        ["Plugins_Author"] = "作者",
        ["Plugins_Path"] = "路径",
        ["Plugins_Loaded"] = "已加载 {0} 个插件",

        ["Home_NewCanvas"] = "+ 新建画布",
        ["Home_OpenFile"] = "📂 打开文件",
        ["Home_RecentProjects"] = "最近项目",
        ["Home_EmptyHint"] = "还没有最近打开的文件\n点击「新建画布」或「打开文件」开始吧",
        ["Home_About"] = "关于 ZeroHush",
        ["Home_Story1"] = "ZeroHush 是一个轻量的 Windows 像素画工具。",
        ["Home_Story2"] = "软件体积小，启动启动快，",
        ["Home_Story3"] = "却能做出不输大型软件的像素作品。",
        ["Home_Story4"] = "零夜 · 从零开始的像素画。",
        ["Home_Story5"] = "一个人，深夜，对着屏幕，",
        ["Home_Story6"] = "一格一格地点亮。",
        ["Home_Studio"] = "由 Zeroμ 工作室出品",
        ["Home_Subtitle"] = "轻量像素画工具",
        ["Home_Footer"] = "开源免费 · 基于 .NET 10 + WPF 构建",

        ["About_Title"] = "关于",
        ["About_Version"] = "v1.0.0-beta",
        ["About_Tagline"] = "轻量像素画工具 · 开源免费",
        ["About_BuiltWith"] = "基于 .NET 10 + WPF 构建",
        ["About_Studio"] = "由 Zeroμ 工作室出品",

        ["Menu_File"] = "文件",
        ["Menu_Edit"] = "编辑",
        ["Menu_Image"] = "图像",
        ["Menu_Layer"] = "图层",
        ["Menu_View"] = "视图",
        ["Menu_Window"] = "窗口",
        ["Menu_Help"] = "帮助",
        ["Menu_Settings"] = "设置",
        ["File_New"] = "新建...",
        ["File_Open"] = "打开...",
        ["File_Save"] = "保存",
        ["File_SaveAs"] = "另存为...",
        ["File_CloseDoc"] = "关闭当前文档",
        ["File_Exit"] = "退出",
        ["Edit_Undo"] = "撤销",
        ["Edit_Redo"] = "重做",
        ["Edit_ClearLayer"] = "清空当前图层",
        ["Image_CanvasSize"] = "画布大小...",
        ["Image_FlipH"] = "水平翻转",
        ["Image_FlipV"] = "垂直翻转",
        ["Layer_New"] = "新建图层",
        ["Layer_NewFolder"] = "新建文件夹",
        ["Layer_Delete"] = "删除当前",
        ["Layer_MoveUp"] = "上移",
        ["Layer_MoveDown"] = "下移",
        ["View_ZoomIn"] = "放大",
        ["View_ZoomOut"] = "缩小",
        ["View_ZoomReset"] = "实际大小",
        ["View_ShowGrid"] = "显示网格",
        ["Window_SwapColors"] = "前景色背景色互换",
        ["Window_NextDoc"] = "下一文档",
        ["Help_Shortcuts"] = "快捷键一览",
        ["Help_About"] = "关于 ZeroHush",

        ["Toolbar_BrushSize"] = "笔刷大小",
        ["Toolbar_Opacity"] = "不透明度",
        ["Toolbar_Mirror"] = "镜像",
        ["Mirror_Off"] = "关",
        ["Mirror_Horizontal"] = "水平",
        ["Mirror_Vertical"] = "垂直",
        ["Mirror_Quadrant"] = "四象限",

        ["Tool_Pen"] = "画笔",
        ["Tool_Eraser"] = "橡皮",
        ["Tool_Fill"] = "油漆桶",
        ["Tool_Picker"] = "吸管",
        ["Tool_Line"] = "直线",
        ["Tool_Rect"] = "矩形",
        ["Tool_Ellipse"] = "椭圆",
        ["Tool_Generic"] = "工具",

        ["Panel_Color"] = "颜色",
        ["Panel_Palette"] = "调色板",
        ["Panel_Layers"] = "图层",
        ["Panel_History"] = "历史记录",
        ["Panel_SaveFormat"] = "保存格式",
        ["Panel_OpenColorPicker"] = "🎨 打开颜色选择器",
        ["Panel_DragSort"] = "拖拽排序 · 右键菜单",
        ["LayerBtn_Add"] = "+图层",
        ["LayerBtn_AddFolder"] = "+文件夹",

        ["Status_Ready"] = "就绪",
        ["Status_SettingsSaved"] = "设置已保存",

        ["Msg_SelectLayer"] = "请先选择一个图层（不能选文件夹）",
        ["Msg_OpenFailed"] = "打开失败：{0}",
        ["Msg_SaveFailed"] = "保存失败：{0}",
        ["Msg_Saved"] = "已保存：{0}",
        ["Msg_CanvasSizeHint"] = "画布尺寸变更请使用「文件 → 新建」重新创建。",
        ["Msg_ErrorTitle"] = "错误",
        ["Msg_InfoTitle"] = "提示",
        ["Msg_FileNotFound"] = "文件不存在，可能已被移动或删除。",
        ["Msg_ConfirmReset"] = "确定要恢复默认设置吗？",

        ["Ctx_Rename"] = "重命名",
        ["Ctx_Delete"] = "删除",
        ["Ctx_AddLayerHere"] = "在此新建图层",
        ["Ctx_AddFolderHere"] = "在此新建子文件夹",

        ["Status_MovedInto"] = "已移入「{0}」",
        ["Status_MovedTo"] = "已移动到「{0}」{1}",
        ["Status_Before"] = "之前",
        ["Status_After"] = "之后",

        ["NewCanvas_Title"] = "新建画布",
        ["NewCanvas_PresetRatio"] = "预设比例",
        ["NewCanvas_Width"] = "宽度（像素）",
        ["NewCanvas_Height"] = "高度（像素）",
        ["NewCanvas_CommonSizes"] = "常用尺寸：16 / 32 / 64 / 128 / 256",
        ["NewCanvas_BackgroundColor"] = "背景色",
        ["NewCanvas_BgTransparent"] = "透明",
        ["NewCanvas_BgWhite"] = "白色",
        ["NewCanvas_BgBlack"] = "黑色",
        ["NewCanvas_BgCustom"] = "自定义",
        ["NewCanvas_PickCustomColor"] = "选择自定义颜色...",

        ["Help_Title"] = "快捷键一览",
        ["Help_Subtitle"] = "ZeroHush 全部键盘操作",
        ["Help_Group_Tools"] = "工具",
        ["Help_Group_Edit"] = "编辑",
        ["Help_Group_File"] = "文件",
        ["Help_Group_View"] = "视图",
        ["Help_Group_Layer"] = "图层",
        ["Help_Pen"] = "画笔",
        ["Help_Eraser"] = "橡皮",
        ["Help_Fill"] = "油漆桶",
        ["Help_Picker"] = "吸管",
        ["Help_Line"] = "直线",
        ["Help_Rect"] = "矩形",
        ["Help_Ellipse"] = "椭圆",
        ["Help_Undo"] = "撤销",
        ["Help_Redo"] = "重做",
        ["Help_SwapColors"] = "交换前景/背景色",
        ["Help_New"] = "新建",
        ["Help_Open"] = "打开",
        ["Help_Save"] = "保存",
        ["Help_CloseDoc"] = "关闭当前文档",
        ["Help_Zoom"] = "缩放",
        ["Help_ScrollH"] = "水平滚动",
        ["Help_BrushSizeAlt"] = "调笔刷大小",
        ["Help_SwitchDoc"] = "切换文档",
        ["Help_LayerHint"] = "拖拽可排序 / 移入文件夹",
        ["Help_LayerHint2"] = "双击图层名可重命名",
        ["Help_LayerHint3"] = "右键图层可弹出菜单",
        ["Help_Wheel"] = "滚轮",

        ["Thanks_Title"] = "致谢",
        ["Thanks_Subtitle"] = "感谢所有帮助过这个项目的朋友",
        ["Thanks_Count"] = "共 {0} 位",
        ["Thanks_CountFiltered"] = "找到 {0} 位匹配",
        ["Thanks_Footer"] = "感谢每一位参与测试和反馈的朋友",
        ["Thanks_Tip"] = "点击打开 B 站主页",
        ["Thanks_TipSearch"] = "点击在 B 站搜索",

        ["Rename_Title"] = "重命名",
        ["Rename_Label"] = "新名称",

        ["Color_Title"] = "选择颜色",
        ["Color_HexLabel"] = "十六进制（#RRGGBB）",

        ["Restart_Title"] = "语言已切换",
        ["Restart_Message"] = "语言设置已保存。\n重启软件后新语言才会生效。",
        ["Restart_Now"] = "立即重启",
        ["Restart_Later"] = "稍后重启",
        ["Env_Title"] = "系统环境不受支持",
        ["Env_Message"] = "您当前的系统环境不能正常运行 ZeroHush。\n\n请升级到 Windows 10（1809）或更高的操作系统。",
        ["Env_CurrentVersion"] = "当前系统版本：",
        ["Theme_Title"] = "主题色",
        ["Theme_Hint"] = "选择一个主题色，界面立即预览。点保存后永久生效。",
        ["Theme_Dark"] = "深灰（默认）",
        ["Theme_Blue"] = "蓝",
        ["Theme_Purple"] = "紫",
        ["Theme_Pink"] = "粉",
        ["Theme_Green"] = "绿",
        ["Theme_Custom"] = "自定义颜色",
        ["Theme_CustomPick"] = "选择颜色...",
        ["Theme_CustomHint"] = "点击色块选择任意颜色",
        ["Status_MemoryLabel"] = "内存：",

        ["Shortcut_EditTitle"] = "编辑快捷键",
        ["Shortcut_PressHint"] = "请按下新的快捷键组合（按 Esc 取消）",
        ["Shortcut_ResetAll"] = "恢复默认快捷键",
        ["Shortcut_Conflict"] = "该键位已被「{0}」使用，是否覆盖？",
        ["Shortcut_BrushSizeDec"] = "笔刷 -1",
        ["Shortcut_BrushSizeInc"] = "笔刷 +1",
        ["Shortcut_OpacityDec"] = "不透明度 -10%",
        ["Shortcut_OpacityInc"] = "不透明度 +10%",
        ["Help_ZoomIn"] = "放大",
        ["Help_ZoomOut"] = "缩小",
        ["Help_ZoomReset"] = "实际大小",

        ["About_ContactEmail"] = "联系邮箱",
        ["About_EmailHint"] = "复制邮箱，或点击按钮打开邮件客户端。",
        ["About_OpenMail"] = "打开邮件客户端",
        ["About_CopyEmail"] = "复制邮箱",
        ["About_Copied"] = "已复制到剪贴板",

        ["Feedback_Title"] = "问题反馈",
        ["Feedback_Type"] = "问题类型",
        ["Feedback_Type_Bug"] = "Bug（软件出错/崩溃）",
        ["Feedback_Type_Ui"] = "UI / 界面问题",
        ["Feedback_Type_Suggest"] = "功能建议",
        ["Feedback_Type_Other"] = "其他",
        ["Feedback_Desc"] = "详细描述",
        ["Feedback_DescHint"] = "请描述你遇到的问题或建议。越详细越好。",
        ["Feedback_SystemInfo"] = "系统信息（自动收集）",
        ["Feedback_CopyAll"] = "复制全部内容",
        ["Feedback_Copied"] = "已复制",
        ["Feedback_OpenMail"] = "打开邮箱并粘贴",
        ["Feedback_OpenGitHub"] = "去 GitHub 提 Issue",
        ["Feedback_Note"] = "提示：点击「打开邮箱」后，请在邮件正文里粘贴（Ctrl+V）刚复制的内容。",

        ["Update_Title"] = "有新版本可用",
        ["Update_Notes"] = "更新内容",
        ["Update_NoNotes"] = "（此版本未提供更新说明）",
        ["Update_Download"] = "立即下载",
        ["Update_Later"] = "稍后",
        ["Update_Skip"] = "跳过此版本",
        ["Update_AutoCheck"] = "启动时自动检查更新",
        ["Update_CheckNow"] = "立即检查",
        ["Update_Checking"] = "正在检查…",
        ["Update_UpToDate"] = "已是最新版本",
        ["Update_CheckFailed"] = "检查失败，请检查网络连接",
    };

    private static readonly Dictionary<string, string> EnDict = new()
    {
        ["Common_OK"] = "OK",
        ["Common_Cancel"] = "Cancel",
        ["Common_Save"] = "Save",
        ["Common_Close"] = "Close",
        ["Common_Reset"] = "Reset",
        ["Common_Confirm"] = "Confirm",

        ["Settings_Title"] = "Settings",
        ["Settings_General"] = "General",
        ["Settings_Performance"] = "Performance",
        ["Settings_Shortcuts"] = "Shortcuts",
        ["Settings_Plugins"] = "Plugins",
        ["Settings_Thanks"] = "Thanks",
        ["Settings_About"] = "About",
        ["Settings_Personalization"] = "Personalization",
        ["Settings_Language"] = "Language",
        ["Settings_LanguageHint"] = "Restart required to apply the new language",
        ["Settings_ShowGrid"] = "Show grid by default",
        ["Settings_BrushSize"] = "Default brush size",
        ["Settings_Opacity"] = "Default opacity",
        ["Settings_HistoryBudget"] = "History memory budget",
        ["Settings_HistoryHint"] = "Limits history snapshot size only. Overall memory also includes canvas pixels and WPF framework overhead.",
        ["Settings_MemoryEstimate"] = "Estimated undo steps (max 60):",
        ["Settings_Shortcut_Brush"] = "Brush / Eraser",
        ["Settings_Shortcut_Fill"] = "Fill / Picker",
        ["Settings_Shortcut_Shape"] = "Line / Rect",
        ["Settings_Shortcut_Ellipse"] = "Ellipse",
        ["Settings_Shortcut_Undo"] = "Undo / Redo",
        ["Settings_Shortcut_Zoom"] = "Zoom",
        ["Settings_Shortcut_Tab"] = "Switch doc",
        ["Settings_ThanksHint"] = "Thanks to everyone who helped with this project.",
        ["Settings_ViewThanks"] = "View thanks list →",
        ["Settings_Version"] = "ZeroHush v1.0.0-beta",
        ["Settings_Tagline"] = "A lightweight pixel art editor · Free & open source",
        ["Settings_BuiltWith"] = "Built with .NET 10 + WPF",

        ["Plugins_None"] = "No plugins installed",
        ["Plugins_Hint"] = "Drop plugin DLL files into the Plugins folder, then restart the app.",
        ["Plugins_Folder"] = "Open Plugins Folder",
        ["Plugins_Version"] = "Version",
        ["Plugins_Author"] = "Author",
        ["Plugins_Path"] = "Path",
        ["Plugins_Loaded"] = "{0} plugin(s) loaded",

        ["Home_NewCanvas"] = "+ New Canvas",
        ["Home_OpenFile"] = "📂 Open File",
        ["Home_RecentProjects"] = "Recent Projects",
        ["Home_EmptyHint"] = "No recent files yet.\nClick \"New Canvas\" or \"Open File\" to begin.",
        ["Home_About"] = "About ZeroHush",
        ["Home_Story1"] = "ZeroHush is a lightweight pixel art editor for Windows.",
        ["Home_Story2"] = "The software is small and starts up quickly,",
        ["Home_Story3"] = "yet powerful enough for serious pixel art.",
        ["Home_Story4"] = "Zero night · Pixel art from zero.",
        ["Home_Story5"] = "One person, late at night, staring at the screen,",
        ["Home_Story6"] = "lighting one pixel at a time.",
        ["Home_Studio"] = "A Zeroμ Studio project",
        ["Home_Subtitle"] = "A lightweight pixel art editor",
        ["Home_Footer"] = "Free & open source · Built with .NET 10 + WPF",

        ["About_Title"] = "About",
        ["About_Version"] = "v1.0.0-beta",
        ["About_Tagline"] = "A lightweight pixel art editor · Free & open source",
        ["About_BuiltWith"] = "Built with .NET 10 + WPF",
        ["About_Studio"] = "A Zeroμ Studio project",

        ["Menu_File"] = "File",
        ["Menu_Edit"] = "Edit",
        ["Menu_Image"] = "Image",
        ["Menu_Layer"] = "Layer",
        ["Menu_View"] = "View",
        ["Menu_Window"] = "Window",
        ["Menu_Help"] = "Help",
        ["Menu_Settings"] = "Settings",
        ["File_New"] = "New...",
        ["File_Open"] = "Open...",
        ["File_Save"] = "Save",
        ["File_SaveAs"] = "Save As...",
        ["File_CloseDoc"] = "Close Document",
        ["File_Exit"] = "Exit",
        ["Edit_Undo"] = "Undo",
        ["Edit_Redo"] = "Redo",
        ["Edit_ClearLayer"] = "Clear Current Layer",
        ["Image_CanvasSize"] = "Canvas Size...",
        ["Image_FlipH"] = "Flip Horizontal",
        ["Image_FlipV"] = "Flip Vertical",
        ["Layer_New"] = "New Layer",
        ["Layer_NewFolder"] = "New Folder",
        ["Layer_Delete"] = "Delete Current",
        ["Layer_MoveUp"] = "Move Up",
        ["Layer_MoveDown"] = "Move Down",
        ["View_ZoomIn"] = "Zoom In",
        ["View_ZoomOut"] = "Zoom Out",
        ["View_ZoomReset"] = "Actual Size",
        ["View_ShowGrid"] = "Show Grid",
        ["Window_SwapColors"] = "Swap Fore / Back",
        ["Window_NextDoc"] = "Next Document",
        ["Help_Shortcuts"] = "Shortcuts",
        ["Help_About"] = "About ZeroHush",

        ["Toolbar_BrushSize"] = "Brush Size",
        ["Toolbar_Opacity"] = "Opacity",
        ["Toolbar_Mirror"] = "Mirror",
        ["Mirror_Off"] = "Off",
        ["Mirror_Horizontal"] = "Horizontal",
        ["Mirror_Vertical"] = "Vertical",
        ["Mirror_Quadrant"] = "Quadrant",

        ["Tool_Pen"] = "Pen",
        ["Tool_Eraser"] = "Eraser",
        ["Tool_Fill"] = "Fill",
        ["Tool_Picker"] = "Picker",
        ["Tool_Line"] = "Line",
        ["Tool_Rect"] = "Rectangle",
        ["Tool_Ellipse"] = "Ellipse",
        ["Tool_Generic"] = "Tool",

        ["Panel_Color"] = "Color",
        ["Panel_Palette"] = "Palette",
        ["Panel_Layers"] = "Layers",
        ["Panel_History"] = "History",
        ["Panel_SaveFormat"] = "Save Format",
        ["Panel_OpenColorPicker"] = "🎨 Open Color Picker",
        ["Panel_DragSort"] = "Drag to sort · Right-click for menu",
        ["LayerBtn_Add"] = "+Layer",
        ["LayerBtn_AddFolder"] = "+Folder",

        ["Status_Ready"] = "Ready",
        ["Status_SettingsSaved"] = "Settings saved",

        ["Msg_SelectLayer"] = "Please select a layer first (folders can't be drawn on)",
        ["Msg_OpenFailed"] = "Open failed: {0}",
        ["Msg_SaveFailed"] = "Save failed: {0}",
        ["Msg_Saved"] = "Saved: {0}",
        ["Msg_CanvasSizeHint"] = "To change canvas size, use File → New to create a new document.",
        ["Msg_ErrorTitle"] = "Error",
        ["Msg_InfoTitle"] = "Info",
        ["Msg_FileNotFound"] = "File not found. It may have been moved or deleted.",
        ["Msg_ConfirmReset"] = "Reset all settings to default?",

        ["Ctx_Rename"] = "Rename",
        ["Ctx_Delete"] = "Delete",
        ["Ctx_AddLayerHere"] = "New Layer Here",
        ["Ctx_AddFolderHere"] = "New Subfolder Here",

        ["Status_MovedInto"] = "Moved into \"{0}\"",
        ["Status_MovedTo"] = "Moved {1} \"{0}\"",
        ["Status_Before"] = "before",
        ["Status_After"] = "after",

        ["NewCanvas_Title"] = "New Canvas",
        ["NewCanvas_PresetRatio"] = "Preset Ratio",
        ["NewCanvas_Width"] = "Width (pixels)",
        ["NewCanvas_Height"] = "Height (pixels)",
        ["NewCanvas_CommonSizes"] = "Common: 16 / 32 / 64 / 128 / 256",
        ["NewCanvas_BackgroundColor"] = "Background",
        ["NewCanvas_BgTransparent"] = "Transparent",
        ["NewCanvas_BgWhite"] = "White",
        ["NewCanvas_BgBlack"] = "Black",
        ["NewCanvas_BgCustom"] = "Custom",
        ["NewCanvas_PickCustomColor"] = "Pick custom color...",

        ["Help_Title"] = "Shortcuts",
        ["Help_Subtitle"] = "All ZeroHush keyboard shortcuts",
        ["Help_Group_Tools"] = "Tools",
        ["Help_Group_Edit"] = "Edit",
        ["Help_Group_File"] = "File",
        ["Help_Group_View"] = "View",
        ["Help_Group_Layer"] = "Layer",
        ["Help_Pen"] = "Pen",
        ["Help_Eraser"] = "Eraser",
        ["Help_Fill"] = "Fill",
        ["Help_Picker"] = "Picker",
        ["Help_Line"] = "Line",
        ["Help_Rect"] = "Rectangle",
        ["Help_Ellipse"] = "Ellipse",
        ["Help_Undo"] = "Undo",
        ["Help_Redo"] = "Redo",
        ["Help_SwapColors"] = "Swap Fore / Back",
        ["Help_New"] = "New",
        ["Help_Open"] = "Open",
        ["Help_Save"] = "Save",
        ["Help_CloseDoc"] = "Close Document",
        ["Help_Zoom"] = "Zoom",
        ["Help_ScrollH"] = "Horizontal Scroll",
        ["Help_BrushSizeAlt"] = "Brush Size",
        ["Help_SwitchDoc"] = "Switch Document",
        ["Help_LayerHint"] = "Drag to sort / drop into folder",
        ["Help_LayerHint2"] = "Double-click layer name to rename",
        ["Help_LayerHint3"] = "Right-click layer for context menu",
        ["Help_Wheel"] = "Wheel",

        ["Thanks_Title"] = "Thanks",
        ["Thanks_Subtitle"] = "Thanks to everyone who helped with this project",
        ["Thanks_Count"] = "{0} people",
        ["Thanks_CountFiltered"] = "{0} matches found",
        ["Thanks_Footer"] = "Thanks to everyone who tested and provided feedback",
        ["Thanks_Tip"] = "Click to open Bilibili page",
        ["Thanks_TipSearch"] = "Click to search on Bilibili",

        ["Rename_Title"] = "Rename",
        ["Rename_Label"] = "New Name",

        ["Color_Title"] = "Choose Color",
        ["Color_HexLabel"] = "Hex (#RRGGBB)",

        ["Restart_Title"] = "Language Changed",
        ["Restart_Message"] = "Your language setting has been saved.\nRestart the app to apply the new language.",
        ["Restart_Now"] = "Restart Now",
        ["Restart_Later"] = "Later",
        ["Env_Title"] = "Unsupported System",
        ["Env_Message"] = "Your current system environment cannot run ZeroHush properly.\n\nPlease upgrade to Windows 10 (1809) or later.",
        ["Env_CurrentVersion"] = "Current system version:",
        ["Theme_Title"] = "Theme Color",
        ["Theme_Hint"] = "Pick a theme color. Preview is live. Save to keep it.",
        ["Theme_Dark"] = "Dark (Default)",
        ["Theme_Blue"] = "Blue",
        ["Theme_Purple"] = "Purple",
        ["Theme_Pink"] = "Pink",
        ["Theme_Green"] = "Green",
        ["Theme_Custom"] = "Custom Color",
        ["Theme_CustomPick"] = "Pick color...",
        ["Theme_CustomHint"] = "Click the swatch to choose any color",
        ["Status_MemoryLabel"] = "Memory: ",

        ["Shortcut_EditTitle"] = "Edit Shortcut",
        ["Shortcut_PressHint"] = "Press the new key combination (Esc to cancel)",
        ["Shortcut_ResetAll"] = "Reset all shortcuts",
        ["Shortcut_Conflict"] = "\"{0}\" already uses this key. Overwrite?",
        ["Shortcut_BrushSizeDec"] = "Brush -1",
        ["Shortcut_BrushSizeInc"] = "Brush +1",
        ["Shortcut_OpacityDec"] = "Opacity -10%",
        ["Shortcut_OpacityInc"] = "Opacity +10%",
        ["Help_ZoomIn"] = "Zoom In",
        ["Help_ZoomOut"] = "Zoom Out",
        ["Help_ZoomReset"] = "Actual Size",

        ["About_ContactEmail"] = "Contact Email",
        ["About_EmailHint"] = "Copy the address, or click the button to open your mail app.",
        ["About_OpenMail"] = "Open Mail App",
        ["About_CopyEmail"] = "Copy Email",
        ["About_Copied"] = "Copied to clipboard",

        ["Feedback_Title"] = "Feedback",
        ["Feedback_Type"] = "Type",
        ["Feedback_Type_Bug"] = "Bug (crash / error)",
        ["Feedback_Type_Ui"] = "UI Issue",
        ["Feedback_Type_Suggest"] = "Feature Suggestion",
        ["Feedback_Type_Other"] = "Other",
        ["Feedback_Desc"] = "Description",
        ["Feedback_DescHint"] = "Describe the issue or suggestion in detail.",
        ["Feedback_SystemInfo"] = "System Info (auto-collected)",
        ["Feedback_CopyAll"] = "Copy All",
        ["Feedback_Copied"] = "Copied",
        ["Feedback_OpenMail"] = "Open Mail & Paste",
        ["Feedback_OpenGitHub"] = "Open GitHub Issue",
        ["Feedback_Note"] = "Tip: After clicking 'Open Mail', paste (Ctrl+V) the copied content into the email body.",

        ["Update_Title"] = "Update Available",
        ["Update_Notes"] = "What's New",
        ["Update_NoNotes"] = "(No release notes for this version)",
        ["Update_Download"] = "Download Now",
        ["Update_Later"] = "Later",
        ["Update_Skip"] = "Skip this version",
        ["Update_AutoCheck"] = "Check for updates on startup",
        ["Update_CheckNow"] = "Check Now",
        ["Update_Checking"] = "Checking...",
        ["Update_UpToDate"] = "You're on the latest version",
        ["Update_CheckFailed"] = "Check failed. Please check your network connection.",
    };
}