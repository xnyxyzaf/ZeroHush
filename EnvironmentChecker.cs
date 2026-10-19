using System;

namespace PixelStudio;

/// <summary>
/// 系统环境检测。
/// </summary>
public static class EnvironmentChecker
{
    // Windows 10 1809 的 Build 号
    private const int MinBuild = 17763;

    /// <summary>当前系统是否满足 ZeroHush 的最低要求</summary>
    public static bool IsSupported()
    {
        try
        {
            var v = Environment.OSVersion.Version;

            // Windows 10 和 11 都返回 10.0.x.x，靠 Build 号区分版本
            if (v.Major == 10)
                return v.Build >= MinBuild;

            // 低于 Windows 10（如 Win7/8）一律不支持
            if (v.Major < 10)
                return false;

            // 假设未来 Windows 主版本号更大，也是支持的
            return true;
        }
        catch
        {
            // 检测失败时假定支持，避免误伤
            return true;
        }
    }

    /// <summary>当前系统的版本字符串，比如 "10.0.19045"</summary>
    public static string GetCurrentVersionString()
    {
        try
        {
            var v = Environment.OSVersion.Version;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
        catch
        {
            return "Unknown";
        }
    }
}