using System;
using System.Windows.Media;

namespace PixelStudio;

public class Layer
{
    public string Name { get; set; }
    public bool Visible { get; set; } = true;

    public int Width { get; }
    public int Height { get; }

    private byte[]? _pixels;

    public bool IsAllocated => _pixels != null;

    public int AllocatedBytes => _pixels?.Length ?? 0;

    public byte[] Pixels
    {
        get
        {
            EnsureAllocated();
            return _pixels!;
        }
        set => _pixels = value;
    }

    public byte[]? PixelsOrNull => _pixels;

    public Layer(int width, int height, string name)
    {
        Name = name;
        Width = width;
        Height = height;
    }

    private void EnsureAllocated()
    {
        if (_pixels == null)
            _pixels = new byte[Width * Height * 4];
    }

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