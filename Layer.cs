using System;
using System.Windows.Media;

namespace PixelStudio;

/// <summary>
/// 单个像素图层（BGRA 格式）。支持延迟分配：
/// 未画过任何像素时，不占用内存。
/// </summary>
public class Layer
{
    public string Name { get; set; }
    public bool Visible { get; set; } = true;

    public int Width { get; }
    public int Height { get; }

    private byte[]? _pixels;

    /// <summary>是否已分配像素内存</summary>
    public bool IsAllocated => _pixels != null;

    /// <summary>已分配的字节数（未分配为 0）</summary>
    public int AllocatedBytes => _pixels?.Length ?? 0;

    /// <summary>
    /// 像素数据（BGRA）。
    /// 读取时若尚未分配，会自动分配一块全 0 内存（透明）。
    /// 外部代码正常读写这个属性即可。
    /// </summary>
    public byte[] Pixels
    {
        get
        {
            EnsureAllocated();
            return _pixels!;
        }
        set => _pixels = value;
    }

    /// <summary>
    /// 内部只读访问：null 表示"这个图层从没被写过，全透明"。
    /// 合成时用这个，可以跳过空图层，避免触发分配。
    /// </summary>
    public byte[]? PixelsOrNull => _pixels;

    public Layer(int width, int height, string name)
    {
        Name = name;
        Width = width;
        Height = height;
        // ★ 延迟分配：这里不 new byte[]，第一次写入才分配
    }

    private void EnsureAllocated()
    {
        if (_pixels == null)
            _pixels = new byte[Width * Height * 4];
    }

    /// <summary>
    /// 克隆像素（用于历史快照）。
    /// 空图层会返回一块全 0 数组，以兼容旧的快照机制。
    /// </summary>
    public byte[] ClonePixels()
    {
        if (_pixels == null)
            return new byte[Width * Height * 4];
        return (byte[])_pixels.Clone();
    }

    public void RestorePixels(byte[] data)
    {
        if (data.Length != Width * Height * 4) return;
        EnsureAllocated();
        Buffer.BlockCopy(data, 0, _pixels!, 0, data.Length);
    }

    /// <summary>
    /// 清空像素。已分配则清零，未分配则什么都不做（本身就是全透明）。
    /// </summary>
    public void Clear()
    {
        if (_pixels == null) return;
        Array.Clear(_pixels, 0, _pixels.Length);
    }

    public void SetPixel(int width, int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;

        EnsureAllocated();
        var px = _pixels!;
        int idx = (y * Width + x) * 4;
        px[idx] = c.B;
        px[idx + 1] = c.G;
        px[idx + 2] = c.R;
        px[idx + 3] = c.A;
    }

    public Color GetPixel(int width, int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return Colors.Transparent;
        if (_pixels == null) return Colors.Transparent;

        int idx = (y * Width + x) * 4;
        return Color.FromArgb(_pixels[idx + 3], _pixels[idx + 2], _pixels[idx + 1], _pixels[idx]);
    }
}