using System;

namespace PixelStudio;

public static class EnvironmentChecker
{
    private const int MinBuild = 17763;

    public static bool IsSupported()
    {
        try
        {
            var v = Environment.OSVersion.Version;

            if (v.Major == 10)
                return v.Build >= MinBuild;

            if (v.Major < 10)
                return false;

            return true;
        }
        catch
        {
            return true;
        }
    }

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