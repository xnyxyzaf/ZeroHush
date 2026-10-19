using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PixelStudio;

/// <summary>
/// 从 GitHub Releases 检查更新。
/// </summary>
public static class UpdateChecker
{
    // ★ 改成你自己的 GitHub 用户名和仓库名
    private const string Owner = "xnyxyzaf";
    private const string Repo = "ZeroHush";

    // ★ 当前版本（跟 About 页一致）
    public const string CurrentVersion = "1.0.0-beta";

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    static UpdateChecker()
    {
        try
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "ZeroHush-UpdateChecker");
            _http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        }
        catch { }
    }

    public class UpdateInfo
    {
        public string LatestVersion { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
    }

    /// <summary>
    /// 检查更新。有新版返回 UpdateInfo，无新版或失败返回 null。
    /// </summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var htmlUrl = root.GetProperty("html_url").GetString() ?? "";
            var body = root.TryGetProperty("body", out var b) ? (b.GetString() ?? "") : "";

            var latest = tag.TrimStart('v', 'V');

            if (IsNewer(latest, CurrentVersion))
            {
                return new UpdateInfo
                {
                    LatestVersion = latest,
                    DownloadUrl = htmlUrl,
                    ReleaseNotes = body
                };
            }
        }
        catch
        {
            // 静默失败：不弹窗、不打扰用户
        }

        return null;
    }

    /// <summary>判断 latest 是否比 current 新</summary>
    private static bool IsNewer(string latest, string current)
    {
        try
        {
            var (ln, ls) = ParseVersion(latest);
            var (cn, cs) = ParseVersion(current);

            for (int i = 0; i < 3; i++)
            {
                if (ln[i] > cn[i]) return true;
                if (ln[i] < cn[i]) return false;
            }

            // 主版本号相同，看后缀：无后缀 > 有后缀
            // 例：1.0.0 > 1.0.0-beta
            if (string.IsNullOrEmpty(ls) && !string.IsNullOrEmpty(cs))
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static (int[] Nums, string Suffix) ParseVersion(string v)
    {
        var dashIdx = v.IndexOf('-');
        string mainPart = v;
        string suffix = "";

        if (dashIdx >= 0)
        {
            mainPart = v.Substring(0, dashIdx);
            suffix = v.Substring(dashIdx + 1);
        }

        var parts = mainPart.Split('.');
        var nums = new int[3];
        for (int i = 0; i < 3 && i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out var n)) nums[i] = n;
        }

        return (nums, suffix);
    }
}